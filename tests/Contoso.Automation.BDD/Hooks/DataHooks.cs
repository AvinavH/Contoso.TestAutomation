using Contoso.Automation.API.Clients;
using Contoso.Automation.BDD.Support;
using Contoso.Automation.Core.Configuration;
using Reqnroll;
using Serilog;

namespace Contoso.Automation.BDD.Hooks;

/// <summary>
/// Manages Dataverse API test data lifecycle for every scenario.
///
/// BeforeScenario (Order=2): seeds precondition entities for scenarios tagged
///   @SeedAccount / @SeedContact / @SeedOpportunity.
///
/// AfterScenario (Order=50): deletes all entities registered in CrmTestContext
///   in FK-safe order (opportunities → contacts → accounts) regardless of outcome.
///
/// Using the API for cleanup — not the UI — keeps teardown fast (~100ms per entity
/// via DELETE request vs ~5s via browser navigation) and resilient to UI failures.
/// </summary>
[Binding]
public sealed class DataHooks
{
    private readonly CrmTestContext        _ctx;
    private readonly AccountApiClient      _accounts;
    private readonly ContactApiClient      _contacts;
    private readonly OpportunityApiClient  _opportunities;
    private readonly ScenarioContext       _scenarioContext;
    private readonly ILogger               _log = Log.ForContext<DataHooks>();

    public DataHooks(
        CrmTestContext ctx,
        AccountApiClient accounts,
        ContactApiClient contacts,
        OpportunityApiClient opportunities,
        ScenarioContext scenarioContext)
    {
        _ctx            = ctx;
        _accounts       = accounts;
        _contacts       = contacts;
        _opportunities  = opportunities;
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario("SeedAccount", Order = 2)]
    public async Task SeedAccountAsync()
    {
        var account = _ctx.CurrentAccount;
        if (account is null)
        {
            _log.Warning("@SeedAccount tag present but CurrentAccount not set — skipping seed");
            return;
        }
        _log.Information("Seeding Account: {Name}", account.Name);
        account.Id = await _accounts.CreateAsync(account);
        _ctx.RegisterForCleanup("accounts", account.Id.Value);
    }

    [BeforeScenario("SeedContact", Order = 3)]
    public async Task SeedContactAsync()
    {
        var contact = _ctx.CurrentContact;
        if (contact is null) return;
        _log.Information("Seeding Contact: {Name}", contact.FullName);
        contact.Id = await _contacts.CreateAsync(contact);
        _ctx.RegisterForCleanup("contacts", contact.Id.Value);
    }

    /// <summary>
    /// Deletes all entities created during the scenario. Runs after every scenario
    /// regardless of pass/fail. Errors are logged and swallowed — cleanup must never
    /// itself cause a test failure.
    /// </summary>
    [AfterScenario(Order = 50)]
    public async Task CleanupTestDataAsync()
    {
        var toDelete = _ctx.GetEntitiesToClean().ToList();
        if (!toDelete.Any()) return;

        _log.Information("Cleaning up {Count} entities for: {Title}",
            toDelete.Count, _scenarioContext.ScenarioInfo.Title);

        foreach (var (entitySet, id) in toDelete)
        {
            try   { await DeleteEntityAsync(entitySet, id); }
            catch (Exception ex)
            { _log.Warning(ex, "Failed to delete {Entity}({Id}) — manual cleanup may be needed", entitySet, id); }
        }
    }

    private async Task DeleteEntityAsync(string entitySet, Guid id)
    {
        switch (entitySet)
        {
            case "opportunities": await _opportunities.DeleteAsync(id); break;
            case "contacts":      await _contacts.DeleteAsync(id);      break;
            case "accounts":      await _accounts.DeleteAsync(id);      break;
            default:
                _log.Warning("No typed delete client for '{EntitySet}'", entitySet);
                break;
        }
    }
}
