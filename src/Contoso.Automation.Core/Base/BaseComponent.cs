using Microsoft.Playwright;
using Contoso.Automation.Core.Configuration;
using Serilog;

namespace Contoso.Automation.Core.Base;

/// <summary>
/// Abstract base for reusable UI components (grids, forms, command bars, lookups).
/// Components are self-contained interaction units scoped within a root locator,
/// which prevents locator ambiguity when multiple similar controls exist on one page.
///
/// Usage pattern:
///   var grid = new D365GridComponent(page, config);
///   await grid.WaitForLoadAsync();
///   await grid.OpenRecordByNameAsync("Demo Test Account");
/// </summary>
public abstract class BaseComponent
{
    protected readonly IPage Page;
    protected readonly TestConfiguration Config;
    protected readonly ILogger Logger;

    protected BaseComponent(IPage page, TestConfiguration config)
    {
        Page   = page;
        Config = config;
        Logger = Log.ForContext(GetType());
    }

    /// <summary>
    /// Safely performs an action with retry. D365 UCI frequently updates the DOM
    /// while Playwright is mid-interaction, causing StaleElementException equivalents.
    /// Three retries with 500ms backoff handles transient DOM mutations gracefully.
    /// </summary>
    protected async Task<T> WithRetryAsync<T>(Func<Task<T>> action, int retries = 3, int delayMs = 500)
    {
        Exception? last = null;
        for (int i = 0; i < retries; i++)
        {
            try
            {
                return await action();
            }
            catch (PlaywrightException ex) when (i < retries - 1)
            {
                last = ex;
                Logger.Warning("Retry {Attempt}/{Max} after Playwright error: {Error}", i + 1, retries, ex.Message);
                await Task.Delay(delayMs * (i + 1));
            }
        }
        throw last!;
    }

    protected async Task WithRetryAsync(Func<Task> action, int retries = 3, int delayMs = 500)
    {
        await WithRetryAsync<bool>(async () => { await action(); return true; }, retries, delayMs);
    }

    /// <summary>
    /// Waits for a locator to be visible within the component's scope.
    /// </summary>
    protected async Task WaitForVisibleAsync(ILocator locator, int timeoutMs = 10_000)
        => await locator.WaitForAsync(new LocatorWaitForOptions
        {
            State   = WaitForSelectorState.Visible,
            Timeout = timeoutMs
        });
}
