using Microsoft.Playwright;
using Contoso.Automation.Core.Base;
using Contoso.Automation.Core.Configuration;
using Contoso.Automation.D365.Components;

namespace Contoso.Automation.D365.Pages.Sales;

/// <summary>Entity list page for Accounts. Wraps D365GridComponent for account-specific operations.</summary>
public sealed class AccountsListPage : BasePage
{
    private readonly D365GridComponent _grid;

    public AccountsListPage(IPage page, TestConfiguration config) : base(page, config)
        => _grid = new D365GridComponent(page, config);

    public async Task WaitForLoadAsync() => await _grid.WaitForLoadAsync();

    public async Task OpenAccountByNameAsync(string name) => await _grid.OpenRecordByNameAsync(name);

    public async Task SearchForAccountAsync(string term) => await _grid.QuickSearchAsync(term);

    public async Task ClickNewAsync()
    {
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "New Create a new Account record." }).ClickAsync();
        await Page.ClickAsync(D365Selectors.NewButton);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<int> GetAccountCountAsync() => await _grid.GetRowCountAsync();

    public async Task<bool> IsAccountVisibleAsync(string name) => await _grid.IsRecordVisibleAsync(name);

    public async Task<IList<string>> GetVisibleAccountNamesAsync() => await _grid.GetVisibleRecordNamesAsync();
}
