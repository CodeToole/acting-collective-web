using Azure;
using Azure.Data.Tables;

namespace theactingcollective.Models;

/// <summary>
/// Azure Table Storage row shape for a Registration.
/// PartitionKey groups all rows for one class-day event; RowKey is the
/// Registration.Id so lookups by id are a direct point read.
/// </summary>
public class RegistrationEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = string.Empty;
    public string? Referral { get; set; }
    public string RegType { get; set; } = "standard";
    public bool CheckedIn { get; set; }
    public DateTimeOffset? CheckedInAt { get; set; }
    public bool Paid { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool AgeConfirmed { get; set; }
    public bool CommsConsent { get; set; }
    public bool WantsNewsletter { get; set; }

    public Registration ToRegistration() => new()
    {
        Id = RowKey,
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        Phone = Phone,
        ExperienceLevel = ExperienceLevel,
        Referral = Referral,
        RegType = RegType,
        CheckedIn = CheckedIn,
        CheckedInAt = CheckedInAt,
        Paid = Paid,
        PaidAt = PaidAt,
        CreatedAt = CreatedAt,
        AgeConfirmed = AgeConfirmed,
        CommsConsent = CommsConsent,
        WantsNewsletter = WantsNewsletter
    };

    public static RegistrationEntity FromRegistration(Registration registration, string partitionKey) => new()
    {
        PartitionKey = partitionKey,
        RowKey = registration.Id,
        FirstName = registration.FirstName,
        LastName = registration.LastName,
        Email = registration.Email,
        Phone = registration.Phone,
        ExperienceLevel = registration.ExperienceLevel,
        Referral = registration.Referral,
        RegType = registration.RegType,
        CheckedIn = registration.CheckedIn,
        CheckedInAt = registration.CheckedInAt,
        Paid = registration.Paid,
        PaidAt = registration.PaidAt,
        CreatedAt = registration.CreatedAt,
        AgeConfirmed = registration.AgeConfirmed,
        CommsConsent = registration.CommsConsent,
        WantsNewsletter = registration.WantsNewsletter
    };
}
