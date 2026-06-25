using Microsoft.Playwright;
using Contoso.Automation.Core.Base;
using Contoso.Automation.Core.Configuration;

namespace Contoso.Automation.D365.Components;

/// <summary>
/// Encapsulates D365 UCI form field interactions for any entity form.
/// Handles D365's field naming convention (logical attribute names) and the
/// async nature of field updates (each keystroke fires a change event).
///
/// D365 form gotchas handled here:
/// - Fields must be cleared with triple-click before Fill to avoid appending text
/// - Option sets must be selected via the select element's option values
/// - After save, D365 briefly shows a loading overlay - we wait for it to clear
/// - Mandatory fields show an asterisk and error below when left empty on save
/// </summary>
public sealed class D365FormComponent : BaseComponent
{
    public D365FormComponent(IPage page, TestConfiguration config) : base(page, config) { }

    /// <summary>
    /// Sets a text field value. Uses triple-click + fill to reliably replace existing content.
    /// fieldName is the D365 attribute logical name (e.g. 'name', 'telephone1', 'websiteurl').
    /// </summary>
    public async Task SetTextFieldAsync(string fieldName, string value)
    {
        Logger.Debug("Setting field '{Field}' = '{Value}'", fieldName, value);
        var input = Page.Locator(D365Selectors.TextInput(fieldName));

        await WithRetryAsync(async () =>
        {
            await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await input.TripleClickAsync();   // Select all existing text
            await input.FillAsync(value);
            await input.PressAsync("Tab");    // Trigger D365 field change event
        });
    }

    /// <summary>
    /// Gets the current value of a text field.
    /// </summary>
    public async Task<string> GetTextFieldValueAsync(string fieldName)
    {
        var input = Page.Locator(D365Selectors.TextInput(fieldName));
        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        return await input.InputValueAsync();
    }

    /// <summary>
    /// Sets a multiline text area field.
    /// </summary>
    public async Task SetTextAreaAsync(string fieldName, string value)
    {
        var area = Page.Locator(D365Selectors.TextArea(fieldName));
        await area.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await area.TripleClickAsync();
        await area.FillAsync(value);
        await area.PressAsync("Tab");
    }

    /// <summary>
    /// Selects an option by its visible label text in an option set (dropdown) field.
    /// </summary>
    public async Task SetOptionSetAsync(string fieldName, string optionLabel)
    {
        Logger.Debug("Setting option set '{Field}' = '{Option}'", fieldName, optionLabel);
        var select = Page.Locator(D365Selectors.OptionSet(fieldName));
        await select.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await select.SelectOptionAsync(new SelectOptionValue { Label = optionLabel });
        await Page.Keyboard.PressAsync("Tab");
    }

    /// <summary>
    /// Sets a currency field. Accepts decimal values as strings.
    /// </summary>
    public async Task SetCurrencyFieldAsync(string fieldName, string amount)
    {
        var input = Page.Locator(D365Selectors.CurrencyInput(fieldName));
        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await input.TripleClickAsync();
        await input.FillAsync(amount);
        await input.PressAsync("Tab");
    }

    /// <summary>
    /// Sets a date field. value should be in the locale format (e.g. "25/12/2024" for en-GB).
    /// </summary>
    public async Task SetDateFieldAsync(string fieldName, string dateValue)
    {
        var input = Page.Locator(D365Selectors.DateInput(fieldName));
        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await input.TripleClickAsync();
        await input.FillAsync(dateValue);
        await input.PressAsync("Tab");
    }

    /// <summary>
    /// Returns the validation error message for a field, or null if no error is shown.
    /// </summary>
    public async Task<string?> GetFieldErrorAsync(string fieldName)
    {
        var error = Page.Locator(D365Selectors.FieldError(fieldName));
        if (!await error.IsVisibleAsync()) return null;
        return await error.InnerTextAsync();
    }

    /// <summary>
    /// Returns true if the field shows a mandatory indicator (* asterisk).
    /// </summary>
    public async Task<bool> IsFieldMandatoryAsync(string fieldName)
    {
        var label = Page.Locator(D365Selectors.FieldLabel(fieldName));
        var text  = await label.InnerTextAsync();
        return text.Contains('*');
    }

    /// <summary>
    /// Clicks the Save button in the command bar and waits for the save operation to complete.
    /// D365 save fires XHR calls - we wait for network idle before returning.
    /// </summary>
    public async Task SaveAsync()
    {
        Logger.Information("Saving form");
        var saveBtn = Page.Locator(D365Selectors.SaveButton);
        await saveBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 30_000 });
    }

    /// <summary>
    /// Saves and closes the form, returning to the list view.
    /// </summary>
    public async Task SaveAndCloseAsync()
    {
        Logger.Information("Saving and closing form");
        var btn = Page.Locator(D365Selectors.SaveAndCloseButton);
        await btn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 30_000 });
    }

    /// <summary>
    /// Waits for a success notification to appear after a save or action.
    /// Returns the notification message text.
    /// </summary>
    public async Task<string> WaitForSuccessNotificationAsync(int timeoutMs = 15_000)
    {
        var notification = Page.Locator(D365Selectors.SuccessNotification);
        await notification.WaitForAsync(new LocatorWaitForOptions
        {
            State   = WaitForSelectorState.Visible,
            Timeout = timeoutMs
        });
        return await notification.InnerTextAsync();
    }
}
