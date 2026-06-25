using Microsoft.Playwright;
using Contoso.Automation.Core.Base;
using Contoso.Automation.Core.Configuration;
using Contoso.Automation.D365.Components;
using Contoso.Automation.D365.Models;

namespace Contoso.Automation.D365.Pages.Sales;

/// <summary>
/// Page object for the D365 Account entity form.
/// Maps each Account attribute to a typed method using D365FormComponent
/// rather than exposing raw field names, which means refactoring a field
/// name only touches this one file.
///
/// D365 Account logical attribute names referenced here:
///   name         → Account Name (mandatory)
///   telephone1   → Business Phone
///   websiteurl   → Website
///   emailaddress1→ Email
///   address1_city→ City
///   industrycode → Industry (option set)
///   revenue      → Annual Revenue (currency)
///   numberofemployees → Employees (integer)
/// </summary>
public sealed class AccountFormPage : BasePage
{
    private readonly D365FormComponent _form;
    private readonly D365LookupFieldComponent _lookup;

    public AccountFormPage(IPage page, TestConfiguration config) : base(page, config)
    {
        _form   = new D365FormComponent(page, config);
        _lookup = new D365LookupFieldComponent(page, config);
    }

    // ─── Field setters ────────────────────────────────────────────────────────

    public async Task SetAccountNameAsync(string name)
        => await _form.SetTextFieldAsync("name", name);

    public async Task SetPhoneAsync(string phone)
        => await _form.SetTextFieldAsync("telephone1", phone);

    public async Task SetWebsiteAsync(string website)
        => await _form.SetTextFieldAsync("websiteurl", website);

    public async Task SetEmailAsync(string email)
        => await _form.SetTextFieldAsync("emailaddress1", email);

    public async Task SetCityAsync(string city)
        => await _form.SetTextFieldAsync("address1_city", city);

    public async Task SetIndustryAsync(string industryLabel)
        => await _form.SetOptionSetAsync("industrycode", industryLabel);

    public async Task SetAnnualRevenueAsync(string amount)
        => await _form.SetCurrencyFieldAsync("revenue", amount);

    public async Task SetPrimaryContactAsync(string contactName)
        => await _lookup.SetLookupBySearchAsync("primarycontactid", contactName);

    // ─── Field getters ────────────────────────────────────────────────────────

    public async Task<string> GetAccountNameAsync()
        => await _form.GetTextFieldValueAsync("name");

    public async Task<string> GetPhoneAsync()
        => await _form.GetTextFieldValueAsync("telephone1");

    public async Task<string?> GetNameFieldErrorAsync()
        => await _form.GetFieldErrorAsync("name");

    // ─── Bulk operations ──────────────────────────────────────────────────────

    /// <summary>
    /// Populates all fields from an AccountModel, mirroring what a business analyst
    /// would do manually. Unmapped fields on the model are silently skipped.
    /// </summary>
    public async Task FillFormAsync(AccountModel account)
    {
        await SetAccountNameAsync(account.Name);

        if (!string.IsNullOrEmpty(account.Phone))
            await SetPhoneAsync(account.Phone);

        if (!string.IsNullOrEmpty(account.Website))
            await SetWebsiteAsync(account.Website);

        if (!string.IsNullOrEmpty(account.Email))
            await SetEmailAsync(account.Email);

        if (!string.IsNullOrEmpty(account.City))
            await SetCityAsync(account.City);

        if (!string.IsNullOrEmpty(account.Industry))
            await SetIndustryAsync(account.Industry);

        if (!string.IsNullOrEmpty(account.AnnualRevenue))
            await SetAnnualRevenueAsync(account.AnnualRevenue);
    }

    // ─── Form actions ─────────────────────────────────────────────────────────

    public async Task SaveAsync()
        => await _form.SaveAsync();

    public async Task SaveAndCloseAsync()
        => await _form.SaveAndCloseAsync();

    public async Task<string> WaitForSuccessNotificationAsync()
        => await _form.WaitForSuccessNotificationAsync();

    /// <summary>
    /// After save, extracts the record GUID from the current URL.
    /// Used by DataHooks to register the entity for cleanup.
    /// </summary>
    public string GetRecordId()
        => ExtractRecordIdFromUrl();

    public async Task WaitForFormLoadAsync()
    {
        await Page.WaitForSelectorAsync(
            D365Selectors.TextInput("name"),
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await WaitForNetworkIdleAsync();
    }
}
