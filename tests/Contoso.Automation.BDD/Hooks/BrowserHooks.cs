using Contoso.Automation.BDD.Support;
using Contoso.Automation.Core.Drivers;
using Contoso.Automation.D365.Authentication;
using Reqnroll;
using Reqnroll.BoDi;
using Serilog;

namespace Contoso.Automation.BDD.Hooks;

/// <summary>
/// Manages Playwright browser context lifecycle per scenario.
/// Order=1 — runs after DependencyInjection (Order=0) has registered all services.
///
/// BeforeScenario:
///   1. Calls D365AuthManager to obtain a valid StorageState path
///      (performs fresh login if state is missing or expired)
///   2. Initialises PlaywrightDriver with that state — D365 session is loaded instantly
///   3. Registers page objects via DependencyInjection now that IPage exists
///
/// AfterScenario:
///   - Saves Playwright trace on failure (network, DOM snapshots, screenshots in one .zip)
///   - Discards trace on pass to avoid disk bloat during long CI runs
///   - Disposes context and page
/// </summary>
[Binding]
public sealed class BrowserHooks
{
    private readonly IObjectContainer   _container;
    private readonly PlaywrightDriver   _driver;
    private readonly BrowserFactory     _browserFactory;
    private readonly D365AuthManager    _authManager;
    private readonly ScenarioContext    _scenarioContext;
    private readonly ILogger            _log = Log.ForContext<BrowserHooks>();

    public BrowserHooks(
        IObjectContainer container,
        PlaywrightDriver driver,
        BrowserFactory browserFactory,
        D365AuthManager authManager,
        ScenarioContext scenarioContext)
    {
        _container      = container;
        _driver         = driver;
        _browserFactory = browserFactory;
        _authManager    = authManager;
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario(Order = 1)]
    public async Task InitialiseBrowserAsync()
    {
        _log.Information("═══ BeforeScenario: {Title} ═══", _scenarioContext.ScenarioInfo.Title);

        var statePath = await _authManager.EnsureAuthenticatedAsync();
        await _driver.InitialiseAsync(_browserFactory, statePath);

        var di = _container.Resolve<DependencyInjection>();
        di.RegisterPageObjects(_driver.Page);
    }

    [AfterScenario(Order = 100)]
    public async Task TeardownBrowserAsync()
    {
        try
        {
            if (_scenarioContext.TestError is not null)
            {
                _log.Warning("Scenario FAILED — saving Playwright trace");
                await _driver.SaveTraceAsync(_scenarioContext.ScenarioInfo.Title);
            }
            else
            {
                await _driver.DiscardTraceAsync();
            }
        }
        finally
        {
            await _driver.DisposeAsync();
            await _browserFactory.DisposeAsync();
        }
    }
}
