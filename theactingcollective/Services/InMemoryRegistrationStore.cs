using System.Collections.Concurrent;
using theactingcollective.Models;

namespace theactingcollective.Services;

/// <summary>
/// Mock, in-memory implementation for the build phase.
/// Thread-safe so multiple SignalR circuits don't stomp each other.
/// Swap for AzureTableRegistrationStore before go-live â€” nothing else changes.
/// </summary>
public class InMemoryRegistrationStore : IRegistrationStore
{
    private readonly ConcurrentDictionary<string, Registration> _data = new();

    public InMemoryRegistrationStore() => Seed();

    public Task<Registration> AddAsync(Registration registration)
    {
        _data[registration.Id] = registration;
        return Task.FromResult(registration);
    }

    public Task<IReadOnlyList<Registration>> GetAllAsync()
        => Task.FromResult<IReadOnlyList<Registration>>(
            _data.Values.OrderBy(r => r.CreatedAt).ToList());

    public Task<Registration?> FindByContactAsync(string emailOrPhone)
    {
        var term = (emailOrPhone ?? string.Empty).Trim().ToLowerInvariant();
        var digits = new string(term.Where(char.IsDigit).ToArray());

        var match = _data.Values.FirstOrDefault(r =>
            r.Email.ToLowerInvariant() == term ||
            (digits.Length >= 4 && new string(r.Phone.Where(char.IsDigit).ToArray()).Contains(digits)));

        return Task.FromResult(match);
    }

    public Task<Registration?> CheckInAsync(string id)
    {
        if (_data.TryGetValue(id, out var reg))
        {
            reg.CheckedIn = true;
            reg.CheckedInAt = DateTimeOffset.Now;
        }
        return Task.FromResult(_data.GetValueOrDefault(id));
    }

    public Task<Registration?> SetPaidAsync(string id, bool paid)
    {
        if (_data.TryGetValue(id, out var reg))
        {
            reg.Paid = paid;
            reg.PaidAt = paid ? DateTimeOffset.Now : null;
        }
        return Task.FromResult(_data.GetValueOrDefault(id));
    }

    public Task<Registration> AddWalkInAsync(string fullName, string? contact)
    {
        var parts = fullName.Trim().Split(' ', 2);
        var reg = new Registration
        {
            FirstName = parts.ElementAtOrDefault(0) ?? fullName,
            LastName = parts.ElementAtOrDefault(1) ?? string.Empty,
            Email = contact ?? "walkin@onsite",
            Phone = contact ?? "â€”",
            ExperienceLevel = "Brand New",
            RegType = "walkin",
            CheckedIn = true,
            CheckedInAt = DateTimeOffset.Now,
            AgeConfirmed = true,
            CommsConsent = true
        };
        _data[reg.Id] = reg;
        return Task.FromResult(reg);
    }

    public Task<Registration> AddToWaitlistAsync(string fullName, string email)
    {
        var parts = (fullName ?? string.Empty).Trim().Split(' ', 2);
        var reg = new Registration
        {
            FirstName = parts.ElementAtOrDefault(0) ?? fullName ?? string.Empty,
            LastName = parts.ElementAtOrDefault(1) ?? string.Empty,
            Email = (email ?? string.Empty).Trim(),
            Phone = "N/A",
            ExperienceLevel = "Newsletter",
            RegType = "waitlist",
            WantsNewsletter = true,
            AgeConfirmed = true,
            CommsConsent = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _data[reg.Id] = reg;
        return Task.FromResult(reg);
    }

    // Seed data mirrors the Stitch staff-dashboard mockup so the UI looks real in dev.
    private void Seed()
    {
        AddSeed("Elias", "Vance", "elias.v@example.com", "+1 (555) 019-2834", "standard", true,  "18:45:22");
        AddSeed("Maya",  "Lin",   "m.lin.act@example.com", "+1 (555) 837-1102", "vip",    false, null);
        AddSeed("Julian","Thorne","j.thorne@example.com", "+1 (555) 293-8841", "walkin", true,  "18:50:04");
        AddSeed("Sasha", "Reed",  "s.reed@example.com",   "+1 (555) 461-7720", "standard", false, null);
        AddSeed("Andre", "Cole",  "a.cole@example.com",   "+1 (555) 902-5518", "standard", true,  "18:41:10");
    }

    private void AddSeed(string first, string last, string email, string phone,
                         string type, bool checkedIn, string? time)
    {
        var reg = new Registration
        {
            FirstName = first, LastName = last, Email = email, Phone = phone,
            ExperienceLevel = "Some Classes or Workshops", RegType = type,
            AgeConfirmed = true, CommsConsent = true,
            CheckedIn = checkedIn
        };
        if (checkedIn && time is not null && TimeOnly.TryParse(time, out var t))
            reg.CheckedInAt = DateTime.Today.Add(t.ToTimeSpan());
        _data[reg.Id] = reg;
    }
}

