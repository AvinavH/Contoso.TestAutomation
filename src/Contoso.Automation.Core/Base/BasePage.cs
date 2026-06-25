using Microsoft.Playwright;
using Contoso.Automation.Core.Configuration;
using Contoso.Automation.Core.Helpers;
using Serilog;

namespace Contoso.Automation.Core.Base;

/// <summary>
/// Abstract base for all page objects. Provides the active IPage, configuration,
/// and shared utilities (screenshots, waits, URL assertions).
///
/// Page objects inherit this but own zero Playwright locators at this level -
/// all selectors live in concrete pages or reusable components. This keeps
/// BasePage stable while pages evolve independently.
/// </summary>
public abstract class BasePage
{
    protected readonly IPage Page;
    protected readonly TestConfiguration Config;
    protected readonly ILogger Logger;

    protected BasePage(IPage page, TestConfiguration config)
    {
        Page   = page;
        Config = config;
        Logger = Log.ForContext(GetType());
    }

    /// <summary>
    /// Waits for the network to settle after an action. D365 UCI fires many XHR
    /// requests on form load / save - this prevents asserting before data arrives.
    /// </summary>
    protected async Task WaitForNetworkIdleAsync(int timeoutMs = 10_000)
        => await Page.WaitForLoadStateAsync(LoadState.NetworkIdle,
            new PageWaitForLoadStateOptions { Timeout = timeoutMs });

    /// <summary>
    /// Waits until the page URL matches the expected partial string.
    /// Useful for confirming D365 navigation completed (entity list vs form URL differs).
    /// </summary>
    protected async Task WaitForUrlContainsAsync(string urlFragment, int timeoutMs = 30_000)
        => await Page.WaitForURLAsync(
            url => url.Contains(urlFragment, StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = timeoutMs });

    /// <summary>
    /// Takes a timestamped screenshot and returns the file path.
    /// Used by reporting hooks on failure.
    /// </summary>
    public async Task<string> TakeScreenshotAsync(string label = "screenshot")
    {
        var dir      = Config.Reporting.ScreenshotDirectory;
        Directory.CreateDirectory(dir);

        var fileName = $"{label}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.png";
        var fullPath = Path.Combine(dir, fileName);

        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path     = fullPath,
            FullPage = false   // Full page is slow; viewport is sufficient for most failures
        });

        Logger.Information("Screenshot saved: {Path}", fullPath);
        return fullPath;
    }

    /// <summary>
    /// Returns the GUID from the D365 record URL.
    /// Pattern: ...pagetype=entityrecord&amp;id={guid}&amp;...
    /// </summary>
    protected string ExtractRecordIdFromUrl()
    {
        var url   = Page.Url;
        var match = System.Text.RegularExpressions.Regex.Match(url, @"id=([0-9a-fA-F\-]{36})");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
