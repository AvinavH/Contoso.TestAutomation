using Newtonsoft.Json;
using Contoso.Automation.API.Auth;
using Contoso.Automation.API.Clients;
using Contoso.Automation.Core.Configuration;
using Contoso.Automation.D365.Models;
using Serilog;

namespace Contoso.Automation.API.Clients;

// ─── Account API Client ───────────────────────────────────────────────────────

/// <summary>
/// Typed Dataverse client for Account (account) entity operations.
/// Used by DataHooks to seed precondition data and clean up after each scenario.
/// </summary>
public sealed class AccountApiClient
{
    private readonly DataverseApiClient _api;
    private const string EntitySet = "accounts";
    private readonly ILogger _log = Log.ForContext<AccountApiClient>();

    public AccountApiClient(DataverseApiClient api) => _api = api;

    /// <summary>Creates an Account in Dataverse and returns its GUID.</summary>
    public async Task<Guid> CreateAsync(AccountModel account)
    {
        var payload = new
        {
            name              = account.Name,
            telephone1        = account.Phone,
            websiteurl        = account.Website,
            emailaddress1     = account.Email,
            address1_city     = account.City,
            description       = account.Description,
            numberofemployees = account.NumberOfEmployees
        };

        var id = await _api.CreateAsync(EntitySet, payload);
        _log.Information("Created Account '{Name}' with ID {Id}", account.Name, id);
        return id;
    }

    /// <summary>Finds an account by exact name match. Returns null if not found.</summary>
    public async Task<Guid?> FindByNameAsync(string name)
    {
        var results = await _api.QueryAsync<AccountQueryResult>(
            EntitySet,
            filter: $"name eq '{name}'",
            select: "accountid,name",
            top: 1);

        return results.FirstOrDefault()?.AccountId;
    }

    /// <summary>Permanently deletes an Account. Safe to call even if already deleted.</summary>
    public async Task DeleteAsync(Guid id)
        => await _api.DeleteAsync(EntitySet, id);

    private sealed class AccountQueryResult
    {
        [JsonProperty("accountid")] public Guid AccountId { get; set; }
        [JsonProperty("name")]      public string Name { get; set; } = string.Empty;
    }
}

// ─── Contact API Client ───────────────────────────────────────────────────────

public sealed class ContactApiClient
{
    private readonly DataverseApiClient _api;
    private const string EntitySet = "contacts";
    private readonly ILogger _log = Log.ForContext<ContactApiClient>();

    public ContactApiClient(DataverseApiClient api) => _api = api;

    public async Task<Guid> CreateAsync(ContactModel contact)
    {
        var payload = new Dictionary<string, object?>
        {
            ["firstname"]     = contact.FirstName,
            ["lastname"]      = contact.LastName,
            ["emailaddress1"] = contact.Email,
            ["telephone1"]    = contact.Phone,
            ["jobtitle"]      = contact.JobTitle
        };

        // Bind parent account via OData navigation property if account ID is known
        if (contact.ParentAccountId.HasValue)
            payload["parentcustomerid_account@odata.bind"] = $"/accounts({contact.ParentAccountId:D})";

        var id = await _api.CreateAsync(EntitySet, payload);
        _log.Information("Created Contact '{Name}' with ID {Id}", contact.FullName, id);
        return id;
    }

    public async Task<Guid?> FindByLastNameAsync(string lastName)
    {
        var results = await _api.QueryAsync<ContactQueryResult>(
            EntitySet,
            filter: $"lastname eq '{lastName}'",
            select: "contactid,fullname",
            top: 1);

        return results.FirstOrDefault()?.ContactId;
    }

    public async Task DeleteAsync(Guid id)
        => await _api.DeleteAsync(EntitySet, id);

    private sealed class ContactQueryResult
    {
        [JsonProperty("contactid")] public Guid ContactId { get; set; }
        [JsonProperty("fullname")]  public string FullName { get; set; } = string.Empty;
    }
}

// ─── Opportunity API Client ───────────────────────────────────────────────────

public sealed class OpportunityApiClient
{
    private readonly DataverseApiClient _api;
    private const string EntitySet = "opportunities";
    private readonly ILogger _log = Log.ForContext<OpportunityApiClient>();

    public OpportunityApiClient(DataverseApiClient api) => _api = api;

    public async Task<Guid> CreateAsync(OpportunityModel opportunity)
    {
        var payload = new Dictionary<string, object?>
        {
            ["name"]        = opportunity.Name,
            ["description"] = opportunity.Description
        };

        if (opportunity.AccountId.HasValue)
            payload["parentaccountid@odata.bind"] = $"/accounts({opportunity.AccountId:D})";

        if (opportunity.ContactId.HasValue)
            payload["parentcontactid@odata.bind"] = $"/contacts({opportunity.ContactId:D})";

        if (!string.IsNullOrEmpty(opportunity.EstimatedValue) &&
            decimal.TryParse(opportunity.EstimatedValue, out var revenue))
            payload["estimatedvalue"] = revenue;

        var id = await _api.CreateAsync(EntitySet, payload);
        _log.Information("Created Opportunity '{Name}' with ID {Id}", opportunity.Name, id);
        return id;
    }

    public async Task DeleteAsync(Guid id)
        => await _api.DeleteAsync(EntitySet, id);
}
