using theactingcollective.Models;

namespace theactingcollective.Services;

/// <summary>
/// Storage contract for registrations. We depend on this interface everywhere,
/// so on go-live we just write an AzureTableRegistrationStore that implements it
/// and change ONE line in Program.cs. No page/component has to change.
/// </summary>
public interface IRegistrationStore
{
    Task<Registration> AddAsync(Registration registration);
    Task<IReadOnlyList<Registration>> GetAllAsync();

    /// <summary>Find one registration by exact email OR by phone digits.</summary>
    Task<Registration?> FindByContactAsync(string emailOrPhone);

    /// <summary>Mark a registration as checked in and stamp the time. Returns null if not found.</summary>
    Task<Registration?> CheckInAsync(string id);

    /// <summary>Add an on-site walk-in that is already checked in.</summary>
    Task<Registration> AddWalkInAsync(string fullName, string? contact);
}

