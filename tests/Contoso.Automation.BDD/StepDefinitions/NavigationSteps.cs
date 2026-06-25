using Contoso.Automation.D365.Pages.Common;
using FluentAssertions;
using Reqnroll;
using Serilog;

namespace Contoso.Automation.BDD.StepDefinitions;

/// <summary>
/// Shared navigation steps used across Account, Contact, and Opportunity features.
/// Reqnroll note: [Binding], [Given], [When], [Then] come from 'using Reqnroll;'
/// — the only change from deprecated SpecFlow (TechTalk.SpecFlow).
/// </summary>
[Binding]
public sealed class NavigationSteps
{
    private readonly D365NavigationPage _navigation;
    private readonly ILogger _log = Log.ForContext<NavigationSteps>();

    public NavigationSteps(D365NavigationPage navigation)
        => _navigation = navigation;

    [Given(@"I am authenticated in D365 as a sales representative")]
    [Given(@"I am logged into D365 as a sales representative")]
    public async Task GivenIAmAuthenticatedAsync()
    {
        // Navigate to D365 home and wait for the SPA to fully render before checking
        await _navigation.GoHomeAsync();
        var isLoaded = await _navigation.IsD365LoadedAsync();
        isLoaded.Should().BeTrue("D365 should be accessible after authentication");
        _log.Information("D365 authentication confirmed");
    }

    [Given(@"I navigate to the Accounts module")]
    [When(@"I navigate back to the Accounts module")]
    public async Task NavigateToAccountsAsync()
        => await _navigation.GoToAccountsAsync();

    [Given(@"I navigate to the Contacts module")]
    [When(@"I navigate back to the Contacts module")]
    public async Task NavigateToContactsAsync()
        => await _navigation.GoToContactsAsync();

    [Given(@"I navigate to the Opportunities module")]
    [When(@"I navigate back to the Opportunities module")]
    public async Task NavigateToOpportunitiesAsync()
        => await _navigation.GoToOpportunitiesAsync();
}
