using Azure;
using Azure.Data.Tables;
using theactingcollective.Models;

namespace theactingcollective.Services;

/// <summary>
/// Azure Table Storage implementation of IRegistrationStore. All rows for the
/// event share one partition ("2026-08-30", the class date), and RowKey is
/// the Registration.Id, so a check-in fetch by id is a fast point read
/// instead of a partition scan.
/// </summary>
public class AzureTableRegistrationStore : IRegistrationStore
{
    private const string TableName = "registrations";
    private const string EventPartitionKey = "2026-08-30";

    private readonly TableClient _table;

    public AzureTableRegistrationStore(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TableStorage")
            ?? throw new InvalidOperationException(
                "Missing 'TableStorage' connection string. Add it under ConnectionStrings in appsettings.");

        _table = new TableClient(connectionString, TableName);
        _table.CreateIfNotExists();
    }

    public async Task<Registration> AddAsync(Registration registration)
    {
        var entity = RegistrationEntity.FromRegistration(registration, EventPartitionKey);
        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        return registration;
    }

    public async Task<IReadOnlyList<Registration>> GetAllAsync()
    {
        var results = new List<Registration>();
        await foreach (var entity in _table.QueryAsync<RegistrationEntity>(e => e.PartitionKey == EventPartitionKey))
        {
            results.Add(entity.ToRegistration());
        }

        return results.OrderBy(r => r.CreatedAt).ToList();
    }

    public async Task<Registration?> FindByContactAsync(string emailOrPhone)
    {
        var term = (emailOrPhone ?? string.Empty).Trim().ToLowerInvariant();
        var digits = new string(term.Where(char.IsDigit).ToArray());

        await foreach (var entity in _table.QueryAsync<RegistrationEntity>(e => e.PartitionKey == EventPartitionKey))
        {
            var emailMatches = entity.Email.ToLowerInvariant() == term;
            var phoneMatches = digits.Length >= 4
                && new string(entity.Phone.Where(char.IsDigit).ToArray()).Contains(digits);

            if (emailMatches || phoneMatches)
            {
                return entity.ToRegistration();
            }
        }

        return null;
    }

    public async Task<Registration?> CheckInAsync(string id)
    {
        RegistrationEntity entity;
        try
        {
            var response = await _table.GetEntityAsync<RegistrationEntity>(EventPartitionKey, id);
            entity = response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }

        entity.CheckedIn = true;
        entity.CheckedInAt = DateTimeOffset.Now;

        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        return entity.ToRegistration();
    }

    public async Task<Registration> AddWalkInAsync(string fullName, string? contact)
    {
        var parts = fullName.Trim().Split(' ', 2);
        var registration = new Registration
        {
            FirstName = parts.ElementAtOrDefault(0) ?? fullName,
            LastName = parts.ElementAtOrDefault(1) ?? string.Empty,
            Email = contact ?? "walkin@onsite",
            Phone = contact ?? "N/A",
            ExperienceLevel = "Brand New",
            RegType = "walkin",
            CheckedIn = true,
            CheckedInAt = DateTimeOffset.Now,
            AgeConfirmed = true,
            CommsConsent = true
        };

        var entity = RegistrationEntity.FromRegistration(registration, EventPartitionKey);
        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        return registration;
    }
}
