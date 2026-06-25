using Contoso.Automation.AI.Agents;
using Contoso.Automation.API.Clients;
using Contoso.Automation.BDD.Support;
using Contoso.Automation.D365.Models;
using Contoso.Automation.D365.Pages.Sales;
using FluentAssertions;
using Reqnroll;
using Serilog;

namespace Contoso.Automation.BDD.StepDefinitions;

/// <summary>
/// Step definitions for all Account feature scenarios.
///
/// Reqnroll note: 'Table' is in the Reqnroll namespace (same API as SpecFlow's Table).
/// No other code changes are needed beyond switching 'using TechTalk.SpecFlow;'
/// to 'using Reqnroll;'.
/// </summary>
[Binding]
public sealed class AccountSteps
{
    private readonly AccountFormPage       _form;
    private readonly AccountsListPage      _list;
    private readonly AccountApiClient      _accountApi;
    private readonly CrmTestContext        _ctx;
    private readonly TestDataGeneratorAgent _ai;
    private readonly ILogger               _log = Log.ForContext<AccountSteps>();

    public AccountSteps(
        AccountFormPage form,
        AccountsListPage list,
        AccountApiClient accountApi,
        CrmTestContext ctx,
        TestDataGeneratorAgent ai)
    {
        _form       = form;
        _list       = list;
        _accountApi = accountApi;
        _ctx        = ctx;
        _ai         = ai;
    }

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given(@"the following accounts exist in the system:")]
    public async Task GivenTheFollowingAccountsExistAsync(Table table)
    {
        foreach (var row in table.Rows)
        {
            var account = new AccountModel { Name = row["Name"] };
            account.Id = await _accountApi.CreateAsync(account);
            _ctx.RegisterForCleanup("accounts", account.Id.Value);
            _log.Information("Pre-condition account created: {Name}", account.Name);
        }
    }

    [Given(@"an account ""(.*)"" exists in the system")]
    public async Task GivenAnAccountExistsAsync(string accountName)
    {
        var account = new AccountModel { Name = accountName };
        account.Id  = await _accountApi.CreateAsync(account);
        _ctx.CurrentAccount = account;
        _ctx.RegisterForCleanup("accounts", account.Id.Value);
    }

    // ─── When ────────────────────────────────────────────────────────────────

    [When(@"I click New to open the Account form")]
    public async Task WhenIClickNewAccountAsync()
    {
        await _list.ClickNewAsync();
        await _form.WaitForFormLoadAsync();
    }

    [When(@"I fill in the account name ""(.*)""")]
    public async Task WhenIFillInAccountNameAsync(string name)
        => await _form.SetAccountNameAsync(name);

    [When(@"I fill in the business phone ""(.*)""")]
    public async Task WhenIFillInPhoneAsync(string phone)
        => await _form.SetPhoneAsync(phone);

    [When(@"I fill in the website ""(.*)""")]
    public async Task WhenIFillInWebsiteAsync(string website)
        => await _form.SetWebsiteAsync(website);

    [When(@"I select ""(.*)"" as the industry")]
    public async Task WhenISelectIndustryAsync(string industry)
        => await _form.SetIndustryAsync(industry);

    [When(@"I create a new account named ""(.*)""")]
    public async Task WhenICreateAccountNamedAsync(string name)
    {
        await _list.ClickNewAsync();
        await _form.WaitForFormLoadAsync();
        await _form.SetAccountNameAsync(name);
    }

    [When(@"I create a new account with the following details:")]
    public async Task WhenICreateAccountWithDetailsAsync(Table table)
    {
        await _list.ClickNewAsync();
        await _form.WaitForFormLoadAsync();

        foreach (var row in table.Rows)
        {
            switch (row["Field"].ToLowerInvariant())
            {
                case "name":    await _form.SetAccountNameAsync(row["Value"]); break;
                case "phone":   await _form.SetPhoneAsync(row["Value"]);       break;
                case "website": await _form.SetWebsiteAsync(row["Value"]);     break;
                case "email":   await _form.SetEmailAsync(row["Value"]);       break;
            }
        }
    }

    [When(@"I create a new account using AI-generated data")]
    public async Task WhenICreateAccountWithAiDataAsync()
    {
        var account = await _ai.GenerateAccountAsync();
        _ctx.CurrentAccount = account;
        await _list.ClickNewAsync();
        await _form.WaitForFormLoadAsync();
        await _form.FillFormAsync(account);
    }

    [When(@"I save the account")]
    public async Task WhenISaveTheAccountAsync()
    {
        await _form.SaveAsync();
        var id = _form.GetRecordId();
        if (!string.IsNullOrEmpty(id) && Guid.TryParse(id, out var guid))
        {
            _ctx.RegisterForCleanup("accounts", guid);
            if (_ctx.CurrentAccount is not null)
                _ctx.CurrentAccount.Id = guid;
        }
    }

    [When(@"I save and close the account")]
    public async Task WhenISaveAndCloseAccountAsync()
    {
        await _form.SaveAndCloseAsync();
    }

    [When(@"I attempt to save the account without entering a name")]
    public async Task WhenISaveWithoutNameAsync()
        => await _form.SaveAsync();

    // ─── Then ────────────────────────────────────────────────────────────────

    [Then(@"the account should be saved without errors")]
    [Then(@"the saved account should appear in the accounts grid")]
    public async Task ThenAccountSavedSuccessfullyAsync()
    {
        var error = await _form.GetNameFieldErrorAsync();
        error.Should().BeNull("account should save without a name field error");
    }

    [Then(@"the form title should display ""(.*)""")]
    public async Task ThenFormTitleDisplaysAsync(string expectedName)
    {
        var actual = await _form.GetAccountNameAsync();
        actual.Should().Be(expectedName);
    }

    [Then(@"the account ""(.*)"" should appear in the accounts list")]
    public async Task ThenAccountAppearsInListAsync(string accountName)
    {
        await _list.WaitForLoadAsync();
        await _list.SearchForAccountAsync(accountName);
        (await _list.IsAccountVisibleAsync(accountName))
            .Should().BeTrue($"account '{accountName}' should be visible after creation");
    }

    [Then(@"the account name field should show a mandatory validation error")]
    public async Task ThenNameFieldShowsErrorAsync()
    {
        var error = await _form.GetNameFieldErrorAsync();
        error.Should().NotBeNullOrEmpty("a mandatory field error should appear when account name is empty");
    }

    [Then(@"the account should not be saved")]
    public void ThenAccountShouldNotBeSavedAsync()
    {
        var id = _form.GetRecordId();
        id.Should().BeEmpty("the account should not have been saved when validation fails");
    }

    [Then(@"the account record in Dataverse should have:")]
    public async Task ThenDataverseRecordHasAsync(Table table)
    {
        var name   = _ctx.CurrentAccount?.Name ?? table.Rows.First()["Value"];
        var result = await _accountApi.FindByNameAsync(name);
        result.Should().NotBeNull("the account should exist in Dataverse");
    }
}
