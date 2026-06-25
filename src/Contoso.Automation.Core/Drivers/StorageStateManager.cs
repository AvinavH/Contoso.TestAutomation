using Contoso.Automation.Core.Configuration;
using Serilog;

namespace Contoso.Automation.Core.Drivers;

/// <summary>
/// Manages the lifecycle of Playwright's StorageState JSON file, which persists
/// D365 browser session cookies and localStorage between test runs.
///
/// Flow:
///   1. Test run starts
///   2. StorageStateManager checks whether .auth/d365-state.json exists and is fresh
///   3. If fresh   → BrowserFactory loads it into the new context (no login needed)
///   4. If stale   → D365AuthManager performs fresh login and saves new state
///   5. On failure → state file is deleted so next run forces a fresh login
///
/// This approach avoids MFA prompts (session persists) while ensuring stale
/// sessions don't silently cause navigation failures mid-suite.
/// </summary>
public sealed class StorageStateManager
{
    private readonly D365Settings _settings;
    private readonly string _statePath;
    private readonly int _expiryHours;

    public StorageStateManager(TestConfiguration config)
    {
        _settings    = config.D365;
        _statePath   = config.D365.StorageStatePath;
        _expiryHours = config.D365.StorageStateExpiryHours;
    }

    /// <summary>
    /// Returns the path to the StorageState file if it exists and is younger than
    /// StorageStateExpiryHours; otherwise returns null (triggering a fresh login).
    /// </summary>
    public string? GetValidStatePath()
    {
        if (!File.Exists(_statePath))
        {
            Log.Information("No StorageState found at {Path} - fresh login required", _statePath);
            return null;
        }

        var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(_statePath);
        if (age.TotalHours >= _expiryHours)
        {
            Log.Information("StorageState is {Hours:F1}h old (limit: {Limit}h) - re-authentication required",
                age.TotalHours, _expiryHours);
            return null;
        }

        Log.Debug("Using cached StorageState from {Path} (age: {Age:F1}h)", _statePath, age.TotalHours);
        return _statePath;
    }

    /// <summary>
    /// Invalidates the stored session. Called when login fails or a session-dependent
    /// operation throws an unexpected authentication error mid-run.
    /// </summary>
    public void Invalidate()
    {
        if (File.Exists(_statePath))
        {
            File.Delete(_statePath);
            Log.Warning("StorageState invalidated at {Path}", _statePath);
        }
    }

    /// <summary>Ensures the .auth directory exists before saving state</summary>
    public void EnsureDirectoryExists()
    {
        var dir = Path.GetDirectoryName(_statePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }
}
