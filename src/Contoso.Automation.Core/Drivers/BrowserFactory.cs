using Microsoft.Playwright;
using Contoso.Automation.Core.Configuration;
using Serilog;

namespace Contoso.Automation.Core.Drivers;

/// <summary>
/// Manages the Playwright browser lifecycle. One instance per test run (singleton).
/// Contexts and Pages are created per-scenario via <see cref="PlaywrightDriver"/>.
///
/// Design note: Browser is expensive to create (~200ms); Context is cheap (~5ms).
/// We keep one browser alive for the entire test run and create a fresh isolated
/// Context for each scenario, which gives us test isolation without the launch overhead.
/// </summary>
public sealed class BrowserFactory : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly BrowserSettings _settings;
    private bool _disposed;

    public BrowserFactory(TestConfiguration config)
    {
        _settings = config.Browser;
    }

    /// <summary>
    /// Returns the shared browser instance, launching it if this is the first call.
    /// Thread-safe for parallel scenario execution.
    /// </summary>
    public async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is not null) return _browser;

        _playwright = await Playwright.CreateAsync();

        IBrowserType browserType = _settings.BrowserType.ToLowerInvariant() switch
        {
            "firefox" => _playwright.Firefox,
            "webkit"  => _playwright.Webkit,
            _         => _playwright.Chromium
        };

        Log.Information("Launching {BrowserType} browser (headless={Headless})",
            _settings.BrowserType, _settings.Headless);

        _browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _settings.Headless,
            SlowMo   = _settings.SlowMo,
            Args     = new[] { "--disable-dev-shm-usage", "--no-sandbox" }
        });

        return _browser;
    }

    /// <summary>
    /// Creates a fresh isolated browser context with configured viewport and timeouts.
    /// Optionally loads a stored StorageState for authenticated sessions.
    /// </summary>
    public async Task<IBrowserContext> CreateContextAsync(string? storageStatePath = null)
    {
        var browser = await GetBrowserAsync();

        var options = new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize
            {
                Width  = _settings.ViewportWidth,
                Height = _settings.ViewportHeight
            },
            IgnoreHTTPSErrors = true,
            RecordVideoDir    = _settings.RecordVideo ? "reports/videos/" : null
        };

        // Load saved auth session if one exists and is still valid
        if (!string.IsNullOrEmpty(storageStatePath) && File.Exists(storageStatePath))
        {
            options.StorageStatePath = storageStatePath;
            Log.Debug("Loading StorageState from {Path}", storageStatePath);
        }

        var context = await browser.NewContextAsync(options);

        // Apply global timeouts at context level so every page inherits them
        context.SetDefaultTimeout(_settings.DefaultTimeoutMs);
        context.SetDefaultNavigationTimeout(_settings.NavigationTimeoutMs);

        return context;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_browser is not null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        Log.Debug("BrowserFactory disposed");
    }
}
