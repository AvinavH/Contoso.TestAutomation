using Microsoft.Playwright;
using Contoso.Automation.Core.Base;
using Contoso.Automation.Core.Configuration;

namespace Contoso.Automation.D365.Components;

/// <summary>
/// Handles D365 lookup field interactions (many-to-one relationship pickers).
/// Lookup fields are the most complex D365 UI element: typing fires a search XHR,
/// a dropdown appears with results, clicking an item selects it.
///
/// Gotchas:
/// - The lookup dropdown renders outside the field's container in a portal div
/// - Results can take 2-3s to appear (Dataverse search round-trip)
/// - If exactly one result exists, D365 may auto-select it (no click needed)
/// - Clearing a lookup requires clicking the remove (X) button, not just deleting text
/// </summary>
public sealed class D365LookupFieldComponent : BaseComponent
{
    public D365LookupFieldComponent(IPage page, TestConfiguration config) : base(page, config) { }

    /// <summary>
    /// Types a search term into a lookup field and selects the first matching result.
    /// </summary>
    public async Task SetLookupBySearchAsync(string fieldName, string searchTerm)
    {
        Logger.Debug("Setting lookup '{Field}' by searching for '{Term}'", fieldName, searchTerm);

        var input = Page.Locator(D365Selectors.LookupInput(fieldName));
        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await input.ClickAsync();
        await input.FillAsync(searchTerm);

        // Wait for the results dropdown to populate
        var results = Page.Locator(D365Selectors.LookupResults(fieldName));
        await results.WaitForAsync(new LocatorWaitForOptions
        {
            State   = WaitForSelectorState.Visible,
            Timeout = 15_000
        });

        // Click the first result item
        var firstItem = Page.Locator(D365Selectors.LookupResultItem(fieldName)).First;
        await firstItem.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await firstItem.ClickAsync();

        // Confirm the field is now populated
        await WaitForLookupPopulatedAsync(fieldName);

        Logger.Debug("Lookup '{Field}' set successfully", fieldName);
    }

    /// <summary>
    /// Sets a lookup by selecting the item that exactly matches the given display name.
    /// Use when multiple results may appear and you need a specific one.
    /// </summary>
    public async Task SetLookupByExactNameAsync(string fieldName, string exactName)
    {
        Logger.Debug("Setting lookup '{Field}' to exact match '{Name}'", fieldName, exactName);

        var input = Page.Locator(D365Selectors.LookupInput(fieldName));
        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await input.ClickAsync();
        await input.FillAsync(exactName);

        // Wait for dropdown and find exact match
        var results = Page.Locator(D365Selectors.LookupResults(fieldName));
        await results.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15_000 });

        var exactItem = Page.Locator(D365Selectors.LookupResultItem(fieldName), new PageLocatorOptions { HasText = exactName });
        await exactItem.ClickAsync();

        await WaitForLookupPopulatedAsync(fieldName);
    }

    /// <summary>
    /// Gets the current display value of a lookup field (the selected record name).
    /// </summary>
    public async Task<string> GetLookupValueAsync(string fieldName)
    {
        // The selected lookup value appears as an anchor tag within the field
        var valueLink = Page.Locator($"[data-id='{fieldName}.fieldControl-LookupResultsDropdown_{fieldName}_selected'] a");
        if (!await valueLink.IsVisibleAsync())
            return string.Empty;

        return await valueLink.InnerTextAsync();
    }

    /// <summary>
    /// Clears a lookup field by clicking the remove (×) button.
    /// </summary>
    public async Task ClearLookupAsync(string fieldName)
    {
        Logger.Debug("Clearing lookup '{Field}'", fieldName);
        var removeBtn = Page.Locator($"[data-id='{fieldName}.fieldControl-LookupResultsDropdown_{fieldName}_selected'] button[aria-label*='Remove']");

        if (await removeBtn.IsVisibleAsync())
            await removeBtn.ClickAsync();
    }

    private async Task WaitForLookupPopulatedAsync(string fieldName)
    {
        // After selection, the input should be cleared and a tag/link should appear
        var input = Page.Locator(D365Selectors.LookupInput(fieldName));
        await RetryHelper.WaitUntilAsync(
            async () => (await input.InputValueAsync()).Length == 0,
            timeoutMs: 5_000,
            description: $"Lookup {fieldName} input cleared after selection");
    }
}
