namespace theactingcollective.Services;

public class EventScheduleService : IEventScheduleService
{
    private static readonly DateTimeOffset DefaultDeadline = new(2026, 8, 30, 18, 0, 0, TimeSpan.FromHours(-5));
    private readonly DateTimeOffset _deadline;

    public EventScheduleService(IConfiguration configuration)
    {
        var configuredDeadline = configuration["Event:RegistrationDeadline"];
        if (DateTimeOffset.TryParse(configuredDeadline, out var parsed))
        {
            _deadline = parsed;
        }
        else
        {
            _deadline = DefaultDeadline;
        }
    }

    public DateTimeOffset Deadline => _deadline;

    public bool RegistrationIsOpen => DateTimeOffset.Now < _deadline;

    public TimeSpan TimeRemaining
    {
        get
        {
            var remaining = _deadline - DateTimeOffset.Now;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
}
