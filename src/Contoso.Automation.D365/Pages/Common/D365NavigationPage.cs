using Microsoft.Playwright;
using Contoso.Automation.Core.Base;
using Contoso.Automation.Core.Configuration;

namespace Contoso.Automation.D365.Pages.Common;

/// <summary>
/// Handles navigation within D365 UCI using direct URL construction.
/// URL navigation is faster and more reliable than sitemap clicking because:
/// - Avoids sitemap rendering delays (~1.5s)
/// - Bypasses potential sitemap DOM changes between D365 versions
/// - Allows navigating to specific records by GUID directly
///
/// URL patterns:
///   Entity list : {baseUrl}/main.aspx?etn={logicalName}&pagetype=entitylist
///   New form    : {baseUrl}/main.aspx?etn={logicalName}&pagetype=entityrecord
///   Record form : {baseUrl}/main.aspx?etn={logicalName}&pagetype=entityrecord&id={guid}
/// </summary>
public sealed class D365NavigationPage : BasePage
{
    public D365NavigationPage(IPage page, TestConfiguration config) : base(page, config) { }

    /// <summary>
    /// Navigates to the D365 app and waits for the top navigation bar to appear.
    /// Uses AppUrl (full URL with appid) when configured, otherwise BaseUrl.
    /// Waits for the topbar element rather than network idle — D365 trial orgs
    /// never reach network idle due to continuous background polling.
    /// </summary>
    public async Task GoHomeAsync()
    {
        var url = !string.IsNullOrEmpty(Config.D365.AppUrl) ? Config.D365.AppUrl : Config.D365.BaseUrl;
        await Page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.WaitForSelectorAsync(
            D365.Components.D365Selectors.TopBar,
            new PageWaitForSelectorOptions { Timeout = 60_000 });
    }

    /// <summary>Navigates to the Accounts entity list view</summary>
    public async Task GoToAccountsAsync()
        => await NavigateToEntityListAsync("account");

    /// <summary>Navigates to the Contacts entity list view</summary>
    public async Task GoToContactsAsync()
        => await NavigateToEntityListAsync("contact");

    /// <summary>Navigates to the Opportunities entity list view</summary>
    public async Task GoToOpportunitiesAsync()
        => await NavigateToEntityListAsync("opportunity");

    /// <summary>Navigates to the new Account form</summary>
    public async Task GoToNewAccountAsync()
        => await NavigateToNewFormAsync("account");

    /// <summary>Navigates to the new Contact form</summary>
    public async Task GoToNewContactAsync()
        => await NavigateToNewFormAsync("contact");

    /// <summary>Navigates to the new Opportunity form</summary>
    public async Task GoToNewOpportunityAsync()
        => await NavigateToNewFormAsync("opportunity");

    /// <summary>Navigates directly to an existing record by GUID</summary>
    public async Task GoToRecordAsync(string entityLogicalName, Guid recordId)
    {
        var url = BuildEntityUrl(entityLogicalName, "entityrecord", $"&id={recordId:D}");
        await NavigateAndWaitAsync(url);
    }

    /// <summary>
    /// Navigates to a D365 entity list view by entity logical name.
    /// </summary>
    public async Task NavigateToEntityListAsync(string entityLogicalName)
    {
        var url = BuildEntityUrl(entityLogicalName, "entitylist");
        await NavigateAndWaitAsync(url);
    }

    /// <summary>
    /// Navigates to the new record form for an entity.
    /// </summary>
    public async Task NavigateToNewFormAsync(string entityLogicalName)
    {
        var url = BuildEntityUrl(entityLogicalName, "entityrecord");
        await NavigateAndWaitAsync(url);
    }

    /// <summary>
    /// Verifies D365 has loaded by checking for the top navigation bar.
    /// </summary>
    public async Task<bool> IsD365LoadedAsync()
    {
        try
        {
            await Page.WaitForSelectorAsync(
                D365.Components.D365Selectors.TopBar,
                new PageWaitForSelectorOptions { Timeout = 10_000 });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private string BuildEntityUrl(string entityLogicalName, string pageType, string extra = "")
        => $"{Config.D365.BaseUrl}/main.aspx?etn={entityLogicalName}&pagetype={pageType}{extra}&navbar=on&cmdbar=true";

    private async Task NavigateAndWaitAsync(string url)
    {
        Logger.Information("Navigating to: {Url}", url);
        await Page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitForNetworkIdleAsync(timeoutMs: 20_000);
    }
}
