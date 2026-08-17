namespace theactingcollective.Services;

public interface IEventScheduleService
{
    DateTimeOffset Deadline { get; }
    bool RegistrationIsOpen { get; }
    TimeSpan TimeRemaining { get; }
}
