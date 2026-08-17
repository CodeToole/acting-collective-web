using System.Net;
using Azure;
using Azure.Communication.Email;
using theactingcollective.Models;

namespace theactingcollective.Services;

public class AcsEmailSender : IEmailSender
{
    private readonly EmailClient? _client;
    private readonly string? _senderAddress;
    private readonly string _squarePaymentLink;
    private readonly string _classDetails;
    private readonly ILogger<AcsEmailSender> _logger;

    public AcsEmailSender(IConfiguration configuration, ILogger<AcsEmailSender> logger)
    {
        _logger = logger;

        var connectionString = configuration["Acs:ConnectionString"];
        _senderAddress = configuration["Acs:SenderAddress"];
        _squarePaymentLink = configuration["Event:SquarePaymentLink"] ?? string.Empty;
        _classDetails = configuration["Event:ClassDetails"] ?? "August 30, 2026 / 6:00–9:00 PM CDT · 350 N Broad St., Suite A · Mobile, AL";

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                _client = new EmailClient(connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid Azure Communication Services connection string provided.");
            }
        }
    }

    public async Task SendRegistrationConfirmationAsync(Registration reg)
    {
        if (string.IsNullOrWhiteSpace(reg.Email))
        {
            _logger.LogWarning("Recipient email is missing. Skipping confirmation email for registration ID {Id}.", reg.Id);
            return;
        }

        if (_client == null || string.IsNullOrWhiteSpace(_senderAddress))
        {
            _logger.LogWarning("Acs:ConnectionString or Acs:SenderAddress is not configured. Skipping confirmation email for {Email}.", reg.Email);
            return;
        }

        try
        {
            var html = $"""
                <!DOCTYPE html>
                <html>
                <head><meta charset="utf-8" /></head>
                <body style="font-family: Arial, sans-serif; background-color: #f5f3ee; color: #141414; margin: 0; padding: 32px 16px;">
                  <div style="max-width: 600px; margin: 0 auto; background-color: #ffffff; border: 1px solid #d8d0c1; border-radius: 8px; padding: 32px; box-sizing: border-box;">
                    <p style="letter-spacing: 0.16em; text-transform: uppercase; font-size: 11px; color: #7a6a3f; font-weight: bold; margin: 0 0 12px 0;">The Acting Collective</p>
                    <h2 style="margin: 0 0 16px 0; font-size: 26px; color: #141414;">Hi {WebUtility.HtmlEncode(reg.FirstName)},</h2>
                    <p style="font-size: 16px; line-height: 1.6; margin: 0 0 16px 0;">Thank you for registering! Your spot is being held.</p>

                    <div style="background: #faf8f5; border-left: 4px solid #d5a62e; padding: 14px 18px; margin: 20px 0; border-radius: 4px;">
                      <p style="font-size: 12px; text-transform: uppercase; letter-spacing: 0.08em; color: #7a6a3f; font-weight: bold; margin: 0 0 6px 0;">Class Details</p>
                      <p style="font-size: 15px; line-height: 1.5; margin: 0; color: #141414;">{WebUtility.HtmlEncode(_classDetails)}</p>
                    </div>

                    <p style="font-size: 16px; line-height: 1.6; margin: 0 0 24px 0;">Please complete your payment below to finalize your seat reservation:</p>

                    <p style="text-align: center; margin: 28px 0;">
                      <a href="{WebUtility.HtmlEncode(_squarePaymentLink)}" style="display: inline-block; background-color: #d5a62e; color: #14100a; text-decoration: none; font-weight: bold; font-size: 16px; padding: 14px 28px; border-radius: 4px;">Reserve Your Seat - Pay Here</a>
                    </p>

                    <p style="font-size: 14px; line-height: 1.6; color: #666666; margin: 28px 0 0 0; border-top: 1px solid #e5dfd5; padding-top: 20px;">
                      We look forward to seeing you. Please arrive a few minutes early to check in at the front desk.
                    </p>
                  </div>
                </body>
                </html>
                """;

            var content = new EmailContent("Your Acting Collective Registration Confirmation")
            {
                Html = html,
                PlainText = $"Hi {reg.FirstName},\n\nThank you for registering for The Acting Collective!\n\nClass Details: {_classDetails}\n\nReserve Your Seat - Pay Here: {_squarePaymentLink}\n\nWe look forward to seeing you!"
            };

            var emailMessage = new EmailMessage(
                _senderAddress,
                new EmailRecipients(new[] { new EmailAddress(reg.Email, reg.FullName) }),
                content);

            await _client.SendAsync(WaitUntil.Started, emailMessage);
            _logger.LogInformation("Registration confirmation email dispatched for {Email}.", reg.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send registration confirmation email to {Email}.", reg.Email);
        }
    }
}
