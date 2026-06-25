using Contoso.Automation.D365.Models;

namespace Contoso.Automation.BDD.Support;

/// <summary>
/// Per-scenario state bag. Injected into step definitions and hooks via Reqnroll's
/// IObjectContainer. Tracks created entity IDs so DataHooks can clean them up after
/// the scenario regardless of pass/fail outcome.
///
/// Design: entity IDs (not names) are stored because names could be reused across runs,
/// and we need to delete the exact records this scenario created via the Dataverse API.
/// </summary>
public sealed class CrmTestContext
{
    // ─── Entity tracking for cleanup ──────────────────────────────────────────

    private readonly List<(string EntitySet, Guid Id)> _createdEntities = new();

    /// <summary>Registers an entity for deletion after the scenario completes.</summary>
    public void RegisterForCleanup(string entitySet, Guid id)
        => _createdEntities.Add((entitySet, id));

    /// <summary>
    /// Returns entities in FK-safe deletion order:
    /// opportunities → contacts → accounts (child before parent).
    /// </summary>
    public IEnumerable<(string EntitySet, Guid Id)> GetEntitiesToClean()
        => _createdEntities
            .OrderByDescending(e => CleanupPriority(e.EntitySet))
            .ToList();

    private static int CleanupPriority(string entitySet) => entitySet switch
    {
        "opportunities" => 3,
        "contacts"      => 2,
        "accounts"      => 1,
        _               => 0
    };

    // ─── Scenario working data ─────────────────────────────────────────────────

    public AccountModel?     CurrentAccount     { get; set; }
    public ContactModel?     CurrentContact     { get; set; }
    public OpportunityModel? CurrentOpportunity { get; set; }
    public string?           LastScreenshotPath { get; set; }
    public string?           LastStepError      { get; set; }
}
