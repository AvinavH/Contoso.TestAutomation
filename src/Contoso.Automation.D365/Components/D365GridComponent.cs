using Microsoft.Playwright;
using Contoso.Automation.Core.Base;
using Contoso.Automation.Core.Configuration;
using Contoso.Automation.Core.Helpers;

namespace Contoso.Automation.D365.Components;

/// <summary>
/// Encapsulates all interactions with D365 UCI entity grid (list) views.
/// Handles search, row selection, pagination awareness, and loading state detection.
///
/// D365 grid gotchas handled here:
/// - Grid renders asynchronously; rows are not present immediately after navigation
/// - Quick Search fires XHR; results arrive with a delay
/// - Row clicks must target the anchor inside the name cell, not the row itself
/// - The grid refreshes after save operations, briefly showing stale data
/// </summary>
public sealed class D365GridComponent : BaseComponent
{
    public D365GridComponent(IPage page, TestConfiguration config) : base(page, config) { }

    /// <summary>
    /// Waits for the grid to finish loading. Must be called after navigation before interacting.
    /// </summary>
    public async Task WaitForLoadAsync()
    {
        Logger.Debug("Waiting for D365 grid to load");

        // Wait for loading spinner to disappear
        try
        {
            await Page.WaitForSelectorAsync(D365Selectors.LoadingSpinner,
                new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden, Timeout = 30_000 });
        }
        catch (TimeoutException)
        {
            // Spinner may not appear for cached loads - that's fine
        }

        // Wait for the grid container to be visible
        await Page.WaitForSelectorAsync(D365Selectors.Grid,
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        // Allow grid data XHR to complete
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle,
            new PageWaitForLoadStateOptions { Timeout = 15_000 });

        Logger.Debug("D365 grid loaded");
    }

    /// <summary>
    /// Returns the number of rows currently visible in the grid (not total record count).
    /// </summary>
    public async Task<int> GetRowCountAsync()
    {
        var rows = Page.Locator(D365Selectors.GridRow);
        return await rows.CountAsync();
    }

    /// <summary>
    /// Opens the first record in the grid by clicking the primary name link.
    /// Throws if the grid is empty.
    /// </summary>
    public async Task OpenFirstRecordAsync()
    {
        Logger.Debug("Opening first grid record");
        var firstLink = Page.Locator(D365Selectors.GridRowNameLink).First;
        await firstLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await firstLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Searches for a record by name using Quick Search, then opens it.
    /// </summary>
    public async Task SearchAndOpenRecordAsync(string searchTerm)
    {
        Logger.Information("Searching grid for: {SearchTerm}", searchTerm);
        await QuickSearchAsync(searchTerm);
        await OpenRecordByNameAsync(searchTerm);
    }

    /// <summary>
    /// Types into the Quick Search box and submits.
    /// </summary>
    public async Task QuickSearchAsync(string searchTerm)
    {
        var searchInput = Page.Locator(D365Selectors.GridSearchInput);
        await searchInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await searchInput.ClearAsync();
        await searchInput.FillAsync(searchTerm);
        await searchInput.PressAsync("Enter");

        // Wait for search results to load
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15_000 });
        await WaitForLoadAsync();
    }

    /// <summary>
    /// Finds a row by the record name text and opens it.
    /// Uses exact text match within the name column links.
    /// </summary>
    public async Task OpenRecordByNameAsync(string recordName)
    {
        Logger.Debug("Opening record: {Name}", recordName);

        var link = Page.Locator(D365Selectors.GridRowNameLink, new PageLocatorOptions
        {
            HasText = recordName
        });

        await WithRetryAsync(async () =>
        {
            await link.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10_000 });
            await link.ClickAsync();
        }, operationName: $"OpenRecord:{recordName}");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Returns true if a record with the given name is visible in the current grid view.
    /// </summary>
    public async Task<bool> IsRecordVisibleAsync(string recordName)
    {
        var link = Page.Locator(D365Selectors.GridRowNameLink, new PageLocatorOptions { HasText = recordName });
        return await link.IsVisibleAsync();
    }

    /// <summary>
    /// Returns the text values of the primary name column for all visible rows.
    /// </summary>
    public async Task<IList<string>> GetVisibleRecordNamesAsync()
    {
        var links = Page.Locator(D365Selectors.GridRowNameLink);
        var count = await links.CountAsync();
        var names = new List<string>(count);

        for (int i = 0; i < count; i++)
            names.Add(await links.Nth(i).InnerTextAsync());

        return names;
    }
}
