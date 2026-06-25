using Microsoft.Playwright;
using Contoso.Automation.Core.Configuration;
using Contoso.Automation.Core.Drivers;
using Serilog;

namespace Contoso.Automation.D365.Authentication;

/// <summary>
/// Orchestrates D365 authentication using Playwright StorageState.
///
/// On first run (or after session expiry):
///   1. Creates a headless context (no StorageState)
///   2. Navigates to D365 → redirected to Microsoft login
///   3. Completes the username/password login flow
///   4. Dismisses the "Stay signed in?" prompt
///   5. Waits for D365 UCI to fully load
///   6. Saves StorageState JSON to .auth/d365-state.json
///
/// Subsequent runs load the saved state directly, skipping login entirely.
/// The session is considered stale after StorageStateExpiryHours (default: 8h).
/// </summary>
public sealed class D365AuthManager
{
    private readonly D365Settings _settings;
    private readonly StorageStateManager _stateManager;
    private readonly BrowserFactory _browserFactory;
    private readonly ILogger _log = Log.ForContext<D365AuthManager>();

    public D365AuthManager(TestConfiguration config, BrowserFactory browserFactory)
    {
        _settings       = config.D365;
        _stateManager   = new StorageStateManager(config);
        _browserFactory = browserFactory;
    }

    /// <summary>
    /// Returns a ready-to-use StorageState path. If no valid state exists,
    /// performs a full login and saves the new state first.
    /// </summary>
    public async Task<string?> EnsureAuthenticatedAsync()
    {
        var validPath = _stateManager.GetValidStatePath();
        if (validPath is not null)
            return validPath;

        _log.Information("Performing fresh D365 login for {Username}", _settings.Username);
        await PerformLoginAndSaveStateAsync();
        return _settings.StorageStatePath;
    }

    private async Task PerformLoginAndSaveStateAsync()
    {
        // Use a separate temporary context to avoid polluting the test context
        var context = await _browserFactory.CreateContextAsync(storageStatePath: null);
        var page    = await context.NewPageAsync();

        try
        {
            var loginPage = new D365LoginPage(page, _settings);
            await loginPage.LoginAsync();

            // Save the session state once D365 home has loaded
            _stateManager.EnsureDirectoryExists();
            await context.StorageStateAsync(new BrowserContextStorageStateOptions
            {
                Path = _settings.StorageStatePath
            });

            _log.Information("StorageState saved to {Path}", _settings.StorageStatePath);
        }
        catch (Exception ex)
        {
            _stateManager.Invalidate();
            _log.Error(ex, "D365 login failed - StorageState invalidated");
            throw;
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }
}
