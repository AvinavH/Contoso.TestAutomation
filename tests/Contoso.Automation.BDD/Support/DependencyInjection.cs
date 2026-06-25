using Contoso.Automation.AI.Agents;
using Contoso.Automation.AI.Services;
using Contoso.Automation.API.Auth;
using Contoso.Automation.API.Clients;
using Contoso.Automation.BDD.Support;
using Contoso.Automation.Core.Configuration;
using Contoso.Automation.Core.Drivers;
using Contoso.Automation.D365.Authentication;
using Contoso.Automation.D365.Pages.Common;
using Contoso.Automation.D365.Pages.Sales;
using Microsoft.Playwright;
using Reqnroll;
using Reqnroll.BoDi;

namespace Contoso.Automation.BDD.Support;

/// <summary>
/// Bootstraps the Reqnroll DI container (BoDi) before each scenario.
/// Runs at Order=0, so all services are available when BrowserHooks (Order=1)
/// and DataHooks (Order=2) execute.
///
/// Reqnroll note: The [Binding] attribute and IObjectContainer come from
/// 'using Reqnroll;' and 'using Reqnroll.BoDi;' — the only namespace change
/// from the deprecated SpecFlow (TechTalk.SpecFlow).
/// </summary>
[Binding]
public sealed class DependencyInjection
{
    private readonly IObjectContainer _container;

    public DependencyInjection(IObjectContainer container)
        => _container = container;

    [BeforeScenario(Order = 0)]
    public void RegisterAll()
    {
        // ── Configuration ───────────────────────────────────────────────────
        var config = ConfigurationLoader.Load();
        _container.RegisterInstanceAs(config);

        // ── Test context (per-scenario state bag) ───────────────────────────
        _container.RegisterInstanceAs(new CrmTestContext());

        // ── Browser / Driver ────────────────────────────────────────────────
        var browserFactory = new BrowserFactory(config);
        _container.RegisterInstanceAs(browserFactory);
        _container.RegisterInstanceAs(new PlaywrightDriver(config));

        // ── D365 Auth ───────────────────────────────────────────────────────
        _container.RegisterInstanceAs(new D365AuthManager(config, browserFactory));

        // ── API Clients ─────────────────────────────────────────────────────
        var authClient = new DataverseAuthClient(config);
        var apiClient  = new DataverseApiClient(config, authClient);
        _container.RegisterInstanceAs(authClient);
        _container.RegisterInstanceAs(apiClient);
        _container.RegisterInstanceAs(new AccountApiClient(apiClient));
        _container.RegisterInstanceAs(new ContactApiClient(apiClient));
        _container.RegisterInstanceAs(new OpportunityApiClient(apiClient));

        // ── AI ──────────────────────────────────────────────────────────────
        var aiService = new ClaudeAIService(config);
        _container.RegisterInstanceAs(aiService);
        _container.RegisterInstanceAs(new TestDataGeneratorAgent(aiService));
    }

    /// <summary>
    /// Called from BrowserHooks after PlaywrightDriver.InitialiseAsync() creates
    /// the live IPage. Page objects cannot be registered at Order=0 because IPage
    /// does not exist until the browser context is open.
    /// </summary>
    public void RegisterPageObjects(IPage page)
    {
        var config = _container.Resolve<TestConfiguration>();
        _container.RegisterInstanceAs(page);
        _container.RegisterInstanceAs(new D365NavigationPage(page, config));
        _container.RegisterInstanceAs(new AccountsListPage(page, config));
        _container.RegisterInstanceAs(new AccountFormPage(page, config));
        _container.RegisterInstanceAs(new ContactsListPage(page, config));
        _container.RegisterInstanceAs(new ContactFormPage(page, config));
        _container.RegisterInstanceAs(new OpportunitiesListPage(page, config));
        _container.RegisterInstanceAs(new OpportunityFormPage(page, config));
    }
}
