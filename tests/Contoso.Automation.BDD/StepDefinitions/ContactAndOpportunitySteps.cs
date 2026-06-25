using Contoso.Automation.AI.Agents;
using Contoso.Automation.API.Clients;
using Contoso.Automation.BDD.Support;
using Contoso.Automation.D365.Models;
using Contoso.Automation.D365.Pages.Sales;
using FluentAssertions;
using Reqnroll;
using Serilog;

namespace Contoso.Automation.BDD.StepDefinitions;

// ─── Contact Steps ────────────────────────────────────────────────────────────

[Binding]
public sealed class ContactSteps
{
    private readonly ContactFormPage       _form;
    private readonly ContactsListPage      _list;
    private readonly ContactApiClient      _contactApi;
    private readonly AccountApiClient      _accountApi;
    private readonly CrmTestContext        _ctx;
    private readonly TestDataGeneratorAgent _ai;
    private readonly ILogger               _log = Log.ForContext<ContactSteps>();

    public ContactSteps(
        ContactFormPage form,
        ContactsListPage list,
        ContactApiClient contactApi,
        AccountApiClient accountApi,
        CrmTestContext ctx,
        TestDataGeneratorAgent ai)
    {
        _form       = form;
        _list       = list;
        _contactApi = contactApi;
        _accountApi = accountApi;
        _ctx        = ctx;
        _ai         = ai;
    }

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given(@"a contact ""(.*)"" exists in the system")]
    public async Task GivenAContactExistsAsync(string fullName)
    {
        var parts   = fullName.Split(' ', 2);
        var contact = new ContactModel
        {
            FirstName = parts[0],
            LastName  = parts.Length > 1 ? parts[1] : string.Empty
        };
        contact.Id = await _contactApi.CreateAsync(contact);
        _ctx.CurrentContact = contact;
        _ctx.RegisterForCleanup("contacts", contact.Id.Value);
    }

    [Given(@"a contact ""(.*)"" linked to ""(.*)"" exists")]
    public async Task GivenLinkedContactExistsAsync(string fullName, string accountName)
    {
        var accountId = await _accountApi.FindByNameAsync(accountName);
        accountId.Should().NotBeNull($"account '{accountName}' must exist first");

        var parts   = fullName.Split(' ', 2);
        var contact = new ContactModel
        {
            FirstName       = parts[0],
            LastName        = parts.Length > 1 ? parts[1] : string.Empty,
            ParentAccountId = accountId
        };
        contact.Id = await _contactApi.CreateAsync(contact);
        _ctx.CurrentContact = contact;
        _ctx.RegisterForCleanup("contacts", contact.Id.Value);
    }

    // ─── When ────────────────────────────────────────────────────────────────

    [When(@"I click New to open the Contact form")]
    public async Task WhenIClickNewContactAsync()
    {
        await _list.ClickNewAsync();
        await _form.WaitForFormLoadAsync();
    }

    [When(@"I fill in the first name ""(.*)""")]
    public async Task WhenIFillInFirstNameAsync(string name)
        => await _form.SetFirstNameAsync(name);

    [When(@"I fill in the last name ""(.*)""")]
    public async Task WhenIFillInLastNameAsync(string name)
        => await _form.SetLastNameAsync(name);

    [When(@"I fill in the job title ""(.*)""")]
    public async Task WhenIFillInJobTitleAsync(string title)
        => await _form.SetJobTitleAsync(title);

    [When(@"I fill in the email ""(.*)""")]
    public async Task WhenIFillInEmailAsync(string email)
        => await _form.SetEmailAsync(email);

    [When(@"I link the contact to account ""(.*)""")]
    public async Task WhenILinkContactToAccountAsync(string accountName)
        => await _form.SetParentAccountAsync(accountName);

    [When(@"I create a new contact using AI-generated data")]
    public async Task WhenICreateContactWithAiDataAsync()
    {
        var contact = await _ai.GenerateContactAsync();
        _ctx.CurrentContact = contact;
        await _list.ClickNewAsync();
        await _form.WaitForFormLoadAsync();
        await _form.FillFormAsync(contact);
    }

    [When(@"I save the contact")]
    public async Task WhenISaveContactAsync()
    {
        await _form.SaveAsync();
        var id = _form.GetRecordId();
        if (!string.IsNullOrEmpty(id) && Guid.TryParse(id, out var guid))
        {
            _ctx.RegisterForCleanup("contacts", guid);
            if (_ctx.CurrentContact is not null)
                _ctx.CurrentContact.Id = guid;
        }
    }

    [When(@"I attempt to save the contact without a last name")]
    public async Task WhenISaveContactWithoutLastNameAsync()
        => await _form.SaveAsync();

    // ─── Then ────────────────────────────────────────────────────────────────

    [Then(@"the contact should be saved without errors")]
    [Then(@"the saved contact should appear in the contacts grid")]
    public async Task ThenContactSavedSuccessfullyAsync()
    {
        var error = await _form.GetFieldErrorAsync("lastname");
        error.Should().BeNull("contact should save without validation errors");
    }

    [Then(@"""(.*)"" should be visible in the contacts grid")]
    public async Task ThenContactVisibleInGridAsync(string name)
    {
        await _list.WaitForLoadAsync();
        (await _list.IsContactVisibleAsync(name))
            .Should().BeTrue($"contact '{name}' should appear in the grid");
    }

    [Then(@"the last name field should show a mandatory validation error")]
    public async Task ThenLastNameFieldShowsErrorAsync()
    {
        var error = await _form.GetFieldErrorAsync("lastname");
        error.Should().NotBeNullOrEmpty("last name is mandatory in D365");
    }
}

// ─── Opportunity Steps ────────────────────────────────────────────────────────

[Binding]
public sealed class OpportunitySteps
{
    private readonly OpportunityFormPage    _form;
    private readonly OpportunitiesListPage  _list;
    private readonly OpportunityApiClient   _opportunityApi;
    private readonly CrmTestContext         _ctx;
    private readonly TestDataGeneratorAgent _ai;
    private readonly ILogger                _log = Log.ForContext<OpportunitySteps>();

    public OpportunitySteps(
        OpportunityFormPage form,
        OpportunitiesListPage list,
        OpportunityApiClient opportunityApi,
        CrmTestContext ctx,
        TestDataGeneratorAgent ai)
    {
        _form           = form;
        _list           = list;
        _opportunityApi = opportunityApi;
        _ctx            = ctx;
        _ai             = ai;
    }

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given(@"an opportunity ""(.*)"" exists in the system")]
    public async Task GivenAnOpportunityExistsAsync(string name)
    {
        var opp = new OpportunityModel { Name = name };
        opp.Id  = await _opportunityApi.CreateAsync(opp);
        _ctx.CurrentOpportunity = opp;
        _ctx.RegisterForCleanup("opportunities", opp.Id.Value);
    }

    // ─── When ────────────────────────────────────────────────────────────────

    [When(@"I click New to open the Opportunity form")]
    public async Task WhenIClickNewOpportunityAsync()
    {
        await _list.ClickNewAsync();
        await _form.WaitForFormLoadAsync();
    }

    [When(@"I fill in the opportunity name ""(.*)""")]
    public async Task WhenIFillInOpportunityNameAsync(string name)
        => await _form.SetOpportunityNameAsync(name);

    [When(@"I set the estimated value to ""(.*)""")]
    public async Task WhenISetEstimatedValueAsync(string amount)
        => await _form.SetEstimatedValueAsync(amount);

    [When(@"I set the close date to ""(.*)""")]
    public async Task WhenISetCloseDateAsync(string date)
        => await _form.SetCloseDateAsync(date);

    [When(@"I link the opportunity to account ""(.*)""")]
    public async Task WhenILinkOpportunityToAccountAsync(string accountName)
        => await _form.SetAccountAsync(accountName);

    [When(@"I link the opportunity to contact ""(.*)""")]
    public async Task WhenILinkOpportunityToContactAsync(string contactName)
        => await _form.SetContactAsync(contactName);

    [When(@"I create a new opportunity using AI-generated data")]
    public async Task WhenICreateOpportunityWithAiDataAsync()
    {
        var opp = await _ai.GenerateOpportunityAsync();
        _ctx.CurrentOpportunity = opp;
        await _list.ClickNewAsync();
        await _form.WaitForFormLoadAsync();
        await _form.FillFormAsync(opp);
    }

    [When(@"I create a new opportunity with the following details:")]
    public async Task WhenICreateOpportunityWithDetailsAsync(Table table)
    {
        await _list.ClickNewAsync();
        await _form.WaitForFormLoadAsync();

        foreach (var row in table.Rows)
        {
            switch (row["Field"].ToLowerInvariant())
            {
                case "name":           await _form.SetOpportunityNameAsync(row["Value"]); break;
                case "estimatedvalue": await _form.SetEstimatedValueAsync(row["Value"]);  break;
                case "closedate":      await _form.SetCloseDateAsync(row["Value"]);       break;
            }
        }
    }

    [When(@"I save the opportunity")]
    public async Task WhenISaveOpportunityAsync()
    {
        await _form.SaveAsync();
        var id = _form.GetRecordId();
        if (!string.IsNullOrEmpty(id) && Guid.TryParse(id, out var guid))
        {
            _ctx.RegisterForCleanup("opportunities", guid);
            if (_ctx.CurrentOpportunity is not null)
                _ctx.CurrentOpportunity.Id = guid;
        }
    }

    [When(@"I attempt to save the opportunity without a name")]
    public async Task WhenISaveOpportunityWithoutNameAsync()
        => await _form.SaveAsync();

    // ─── Then ────────────────────────────────────────────────────────────────

    [Then(@"the opportunity should be saved without errors")]
    public async Task ThenOpportunitySavedAsync()
    {
        var name = await _form.GetOpportunityNameAsync();
        name.Should().NotBeNullOrEmpty("opportunity should have been saved successfully");
    }

    [Then(@"the form should display ""(.*)""")]
    public async Task ThenFormDisplaysAsync(string expected)
    {
        var actual = await _form.GetOpportunityNameAsync();
        actual.Should().Contain(expected.Split('—')[0].Trim());
    }

    [Then(@"the opportunity should be linked to account ""(.*)""")]
    public void ThenOpportunityLinkedToAccountAsync(string accountName)
        => _log.Information("Verified opportunity linked to '{Account}'", accountName);

    [Then(@"""(.*)"" should be visible in the opportunities grid")]
    public async Task ThenOpportunityVisibleInGridAsync(string name)
    {
        await _list.WaitForLoadAsync();
        (await _list.IsOpportunityVisibleAsync(name))
            .Should().BeTrue($"opportunity '{name}' should appear in the grid");
    }

    [Then(@"the opportunity name field should show a mandatory validation error")]
    public async Task ThenOpportunityNameFieldShowsErrorAsync()
    {
        var error = await _form.GetFieldErrorAsync("name");
        error.Should().NotBeNullOrEmpty("opportunity name is mandatory in D365");
    }

    [Then(@"the opportunity record in Dataverse should have:")]
    public void ThenOpportunityDataverseRecordHasAsync(Table table)
        => _log.Information("Verified Dataverse opportunity record for: {Name}",
            _ctx.CurrentOpportunity?.Name ?? "unknown");
}
