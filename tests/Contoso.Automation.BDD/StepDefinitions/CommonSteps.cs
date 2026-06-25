using Contoso.Automation.D365.Components;
using Contoso.Automation.D365.Pages.Sales;
using FluentAssertions;
using Microsoft.Playwright;
using Reqnroll;
using Serilog;

namespace Contoso.Automation.BDD.StepDefinitions;

/// <summary>
/// Grid, search, and record-opening steps that are shared across
/// Account, Contact, and Opportunity features.
/// Dispatches to the correct typed list page based on the current URL entity type.
/// </summary>
[Binding]
public sealed class CommonSteps
{
    private readonly AccountsListPage      _accountsList;
    private readonly ContactsListPage      _contactsList;
    private readonly OpportunitiesListPage _opportunitiesList;
    private readonly IPage                 _page;
    private readonly ILogger               _log = Log.ForContext<CommonSteps>();

    public CommonSteps(
        AccountsListPage accountsList,
        ContactsListPage contactsList,
        OpportunitiesListPage opportunitiesList,
        IPage page)
    {
        _accountsList      = accountsList;
        _contactsList      = contactsList;
        _opportunitiesList = opportunitiesList;
        _page              = page;
    }

    [When(@"I search for ""(.*)""")]
    public async Task WhenISearchForAsync(string term)
    {
        if (_page.Url.Contains("etn=account"))
            await _accountsList.SearchForAccountAsync(term);
        else if (_page.Url.Contains("etn=contact"))
            await _contactsList.SearchAsync(term);
        else if (_page.Url.Contains("etn=opportunity"))
            await _opportunitiesList.SearchAsync(term);
        else
            throw new InvalidOperationException($"Unknown entity list URL: {_page.Url}");
    }

    [When(@"I open the record ""(.*)""")]
    public async Task WhenIOpenRecordAsync(string name)
    {
        if (_page.Url.Contains("etn=account"))
            await _accountsList.OpenAccountByNameAsync(name);
        else if (_page.Url.Contains("etn=contact"))
            await _contactsList.OpenContactByNameAsync(name);
        else
            throw new InvalidOperationException($"Cannot open record — unknown entity: {_page.Url}");
    }

    [Then(@"the grid should display at least (\d+) results?")]
    public async Task ThenGridDisplaysAtLeastAsync(int minCount)
    {
        var count = await GetCurrentGridCountAsync();
        count.Should().BeGreaterThanOrEqualTo(minCount);
    }

    [Then(@"the grid should display (\d+) results?")]
    public async Task ThenGridDisplaysExactlyAsync(int expectedCount)
    {
        var count = await GetCurrentGridCountAsync();
        count.Should().Be(expectedCount);
    }

    [Then(@"""(.*)"" should be visible in the grid")]
    public async Task ThenRecordVisibleInGridAsync(string name)
    {
        (await IsRecordVisibleAsync(name))
            .Should().BeTrue($"'{name}' should be visible in the current grid");
    }

    [Then(@"I should be on the Account form for ""(.*)""")]
    public void ThenShouldBeOnAccountFormAsync(string _)
    {
        _page.Url.Should().Contain("pagetype=entityrecord");
        _page.Url.Should().Contain("etn=account");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<int> GetCurrentGridCountAsync()
    {
        if (_page.Url.Contains("etn=account"))
        {
            await _accountsList.WaitForLoadAsync();
            return await _accountsList.GetAccountCountAsync();
        }
        var grid = new D365GridComponent(_page, null!);
        await grid.WaitForLoadAsync();
        return await grid.GetRowCountAsync();
    }

    private async Task<bool> IsRecordVisibleAsync(string name)
    {
        if (_page.Url.Contains("etn=account"))     return await _accountsList.IsAccountVisibleAsync(name);
        if (_page.Url.Contains("etn=contact"))     return await _contactsList.IsContactVisibleAsync(name);
        if (_page.Url.Contains("etn=opportunity")) return await _opportunitiesList.IsOpportunityVisibleAsync(name);
        return false;
    }
}
