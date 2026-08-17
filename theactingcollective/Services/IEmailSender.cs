using theactingcollective.Models;

namespace theactingcollective.Services;

public interface IEmailSender
{
    Task SendRegistrationConfirmationAsync(Registration reg);
}

