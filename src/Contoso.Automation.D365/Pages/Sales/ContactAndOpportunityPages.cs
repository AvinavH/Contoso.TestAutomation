using Microsoft.Playwright;
using Contoso.Automation.Core.Base;
using Contoso.Automation.Core.Configuration;
using Contoso.Automation.D365.Components;
using Contoso.Automation.D365.Models;

namespace Contoso.Automation.D365.Pages.Sales;

// ─── Contact Pages ────────────────────────────────────────────────────────────

public sealed class ContactsListPage : BasePage
{
    private readonly D365GridComponent _grid;

    public ContactsListPage(IPage page, TestConfiguration config) : base(page, config)
        => _grid = new D365GridComponent(page, config);

    public async Task WaitForLoadAsync() => await _grid.WaitForLoadAsync();
    public async Task OpenContactByNameAsync(string name) => await _grid.OpenRecordByNameAsync(name);
    public async Task SearchAsync(string term) => await _grid.QuickSearchAsync(term);
    public async Task<bool> IsContactVisibleAsync(string name) => await _grid.IsRecordVisibleAsync(name);
    public async Task ClickNewAsync()
    {
        await Page.ClickAsync(D365Selectors.NewButton);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}

/// <summary>
/// D365 Contact entity form.
/// Logical attribute names: firstname, lastname, fullname, emailaddress1,
/// telephone1, jobtitle, parentcustomerid (account lookup)
/// </summary>
public sealed class ContactFormPage : BasePage
{
    private readonly D365FormComponent _form;
    private readonly D365LookupFieldComponent _lookup;

    public ContactFormPage(IPage page, TestConfiguration config) : base(page, config)
    {
        _form   = new D365FormComponent(page, config);
        _lookup = new D365LookupFieldComponent(page, config);
    }

    public async Task SetFirstNameAsync(string firstName)
        => await _form.SetTextFieldAsync("firstname", firstName);

    public async Task SetLastNameAsync(string lastName)
        => await _form.SetTextFieldAsync("lastname", lastName);

    public async Task SetEmailAsync(string email)
        => await _form.SetTextFieldAsync("emailaddress1", email);

    public async Task SetJobTitleAsync(string title)
        => await _form.SetTextFieldAsync("jobtitle", title);

    public async Task SetPhoneAsync(string phone)
        => await _form.SetTextFieldAsync("telephone1", phone);

    public async Task SetParentAccountAsync(string accountName)
        => await _lookup.SetLookupBySearchAsync("parentcustomerid", accountName);

    public async Task<string> GetFullNameAsync()
        => await _form.GetTextFieldValueAsync("fullname");

    public async Task FillFormAsync(ContactModel contact)
    {
        await SetFirstNameAsync(contact.FirstName);
        await SetLastNameAsync(contact.LastName);

        if (!string.IsNullOrEmpty(contact.Email))
            await SetEmailAsync(contact.Email);
        if (!string.IsNullOrEmpty(contact.JobTitle))
            await SetJobTitleAsync(contact.JobTitle);
        if (!string.IsNullOrEmpty(contact.Phone))
            await SetPhoneAsync(contact.Phone);
        if (!string.IsNullOrEmpty(contact.ParentAccountName))
            await SetParentAccountAsync(contact.ParentAccountName);
    }

    public async Task<string?> GetFieldErrorAsync(string fieldName) => await _form.GetFieldErrorAsync(fieldName);
    public async Task SaveAsync() => await _form.SaveAsync();
    public async Task SaveAndCloseAsync() => await _form.SaveAndCloseAsync();
    public string GetRecordId() => ExtractRecordIdFromUrl();

    public async Task WaitForFormLoadAsync()
    {
        await Page.WaitForSelectorAsync(
            D365Selectors.TextInput("lastname"),
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await WaitForNetworkIdleAsync();
    }
}

// ─── Opportunity Pages ────────────────────────────────────────────────────────

public sealed class OpportunitiesListPage : BasePage
{
    private readonly D365GridComponent _grid;

    public OpportunitiesListPage(IPage page, TestConfiguration config) : base(page, config)
        => _grid = new D365GridComponent(page, config);

    public async Task WaitForLoadAsync() => await _grid.WaitForLoadAsync();
    public async Task OpenOpportunityByNameAsync(string name) => await _grid.OpenRecordByNameAsync(name);
    public async Task SearchAsync(string term) => await _grid.QuickSearchAsync(term);
    public async Task<bool> IsOpportunityVisibleAsync(string name) => await _grid.IsRecordVisibleAsync(name);
    public async Task ClickNewAsync()
    {
        await Page.ClickAsync(D365Selectors.NewButton);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}

/// <summary>
/// D365 Opportunity entity form.
/// Key attributes: name, parentaccountid (account), parentcontactid (contact),
/// estimatedvalue (currency), estimatedclosedate (date), stepname (stage),
/// closeprobability (integer), description
/// </summary>
public sealed class OpportunityFormPage : BasePage
{
    private readonly D365FormComponent _form;
    private readonly D365LookupFieldComponent _lookup;

    public OpportunityFormPage(IPage page, TestConfiguration config) : base(page, config)
    {
        _form   = new D365FormComponent(page, config);
        _lookup = new D365LookupFieldComponent(page, config);
    }

    public async Task SetOpportunityNameAsync(string name)
        => await _form.SetTextFieldAsync("name", name);

    public async Task SetEstimatedValueAsync(string amount)
        => await _form.SetCurrencyFieldAsync("estimatedvalue", amount);

    public async Task SetCloseDateAsync(string date)
        => await _form.SetDateFieldAsync("estimatedclosedate", date);

    public async Task SetDescriptionAsync(string description)
        => await _form.SetTextAreaAsync("description", description);

    public async Task SetAccountAsync(string accountName)
        => await _lookup.SetLookupBySearchAsync("parentaccountid", accountName);

    public async Task SetContactAsync(string contactName)
        => await _lookup.SetLookupBySearchAsync("parentcontactid", contactName);

    public async Task<string> GetOpportunityNameAsync()
        => await _form.GetTextFieldValueAsync("name");

    public async Task FillFormAsync(OpportunityModel opportunity)
    {
        await SetOpportunityNameAsync(opportunity.Name);

        if (!string.IsNullOrEmpty(opportunity.AccountName))
            await SetAccountAsync(opportunity.AccountName);
        if (!string.IsNullOrEmpty(opportunity.ContactName))
            await SetContactAsync(opportunity.ContactName);
        if (!string.IsNullOrEmpty(opportunity.EstimatedValue))
            await SetEstimatedValueAsync(opportunity.EstimatedValue);
        if (!string.IsNullOrEmpty(opportunity.CloseDate))
            await SetCloseDateAsync(opportunity.CloseDate);
        if (!string.IsNullOrEmpty(opportunity.Description))
            await SetDescriptionAsync(opportunity.Description);
    }

    public async Task<string?> GetFieldErrorAsync(string fieldName) => await _form.GetFieldErrorAsync(fieldName);
    public async Task SaveAsync() => await _form.SaveAsync();
    public async Task SaveAndCloseAsync() => await _form.SaveAndCloseAsync();
    public string GetRecordId() => ExtractRecordIdFromUrl();

    public async Task WaitForFormLoadAsync()
    {
        await Page.WaitForSelectorAsync(
            D365Selectors.TextInput("name"),
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await WaitForNetworkIdleAsync();
    }
}
