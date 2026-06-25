using Microsoft.Playwright;
using Contoso.Automation.Core.Configuration;
using Serilog;

namespace Contoso.Automation.D365.Authentication;

/// <summary>
/// Automates the Microsoft identity login flow that D365 redirects to.
/// The flow navigates through login.microsoftonline.com, which presents:
///   Step 1 → Email entry screen
///   Step 2 → Password entry screen
///   Step 3 → "Stay signed in?" prompt (we accept to maximise session lifetime)
///   Step 4 → D365 UCI home loads
///
/// Selectors target stable Microsoft identity platform HTML attributes.
/// If MFA is enabled on the tenant, this flow will fail - the account must
/// have MFA disabled or use a conditional access policy exclusion for automation.
/// </summary>
public sealed class D365LoginPage
{
    private readonly IPage _page;
    private readonly D365Settings _settings;
    private readonly ILogger _log = Log.ForContext<D365LoginPage>();

    // Microsoft identity platform selectors (stable across tenants)
    private const string EmailInput    = "input[type='email'], #i0116";
    private const string NextButton    = "input[id='idSIButton9'], button[type='submit']";
    private const string PasswordInput = "input[type='password'], #i0118";
    private const string SignInButton  = "input[id='idSIButton9'], #idSIButton9";
    private const string StaySignedIn  = "#idSIButton9";           // "Yes" on KMSI screen
    private const string KmsiScreen   = "#KmsiCheckboxLabel, #kmsiTitle";

    // D365 UCI load indicator - appears when the shell has rendered
    private const string D365Header   = "div[data-id='topbar'], .o365cs-nav-topbar-container, .ms-Nav-compositeLink";

    public D365LoginPage(IPage page, D365Settings settings)
    {
        _page     = page;
        _settings = settings;
    }

    /// <summary>
    /// Executes the full login sequence: navigate → enter credentials → handle KMSI → wait for D365.
    /// </summary>
    public async Task LoginAsync()
    {
        _log.Information("Navigating to D365: {Url}", _settings.BaseUrl);
        await _page.GotoAsync(_settings.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Wait for redirect to Microsoft login
        await _page.WaitForURLAsync(url => url.Contains("login.microsoftonline.com") || url.Contains(_settings.BaseUrl),
            new PageWaitForURLOptions { Timeout = 30_000 });

        // If already redirected to D365, session might still be live
        if (_page.Url.Contains(_settings.BaseUrl) && !_page.Url.Contains("login.microsoftonline.com"))
        {
            _log.Information("Already authenticated - no login required");
            await WaitForD365LoadAsync();
            return;
        }

        await EnterUsernameAsync();
        await EnterPasswordAsync();
        await HandleStaySignedInAsync();
        await WaitForD365LoadAsync();

        _log.Information("D365 login completed successfully");
    }

    private async Task EnterUsernameAsync()
    {
        _log.Debug("Entering username");
        await _page.WaitForSelectorAsync(EmailInput, new PageWaitForSelectorOptions { Timeout = 15_000 });
        await _page.FillAsync(EmailInput, _settings.Username);
        await _page.ClickAsync(NextButton);
        await _page.WaitForTimeoutAsync(1_000); // Brief pause for transition animation
    }

    private async Task EnterPasswordAsync()
    {
        _log.Debug("Entering password");
        await _page.WaitForSelectorAsync(PasswordInput, new PageWaitForSelectorOptions { Timeout = 15_000 });
        await _page.FillAsync(PasswordInput, _settings.Password);
        await _page.ClickAsync(SignInButton);
    }

    private async Task HandleStaySignedInAsync()
    {
        // The KMSI screen appears after login; dismissing with "Yes" maximises session lifetime
        try
        {
            //await _page.WaitForSelectorAsync(KmsiScreen, new PageWaitForSelectorOptions { Timeout = 8_000 });
            _log.Debug("Accepting 'Stay signed in?' prompt");
            await _page.ClickAsync(StaySignedIn);
        }
        catch (TimeoutException)
        {
            // KMSI screen is optional depending on tenant conditional access settings
            _log.Debug("'Stay signed in?' prompt did not appear - continuing");
        }
    }

    private async Task WaitForD365LoadAsync()
    {
        _log.Debug("Waiting for D365 UCI to load");
        await _page.WaitForSelectorAsync(D365Header, new PageWaitForSelectorOptions
        {
            Timeout = _settings.StorageStateExpiryHours * 1000 > 30_000
                ? 60_000
                : 60_000
        });
        // Allow D365 background XHR calls to complete before saving state
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 30_000 });
    }
}
