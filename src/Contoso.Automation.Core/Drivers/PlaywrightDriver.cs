using Microsoft.Playwright;
using Contoso.Automation.Core.Configuration;
using Serilog;

namespace Contoso.Automation.Core.Drivers;

/// <summary>
/// Per-scenario Playwright driver. Holds the active IPage and IBrowserContext
/// for the current test, manages trace recording, and ensures clean teardown.
///
/// Injected via Reqnroll's IObjectContainer from BrowserHooks so every step
/// definition receives the same page instance within a scenario.
/// </summary>
public sealed class PlaywrightDriver : IAsyncDisposable
{
    private IBrowserContext? _context;
    private IPage? _page;
    private bool _traceStarted;
    private readonly BrowserSettings _settings;
    private readonly ReportingSettings _reporting;

    public PlaywrightDriver(TestConfiguration config)
    {
        _settings  = config.Browser;
        _reporting = config.Reporting;
    }

    /// <summary>The active page - throws if InitialiseAsync has not been called</summary>
    public IPage Page => _page ?? throw new InvalidOperationException(
        "PlaywrightDriver has not been initialised. Ensure BrowserHooks.BeforeScenario ran first.");

    /// <summary>The active context - used by StorageStateManager to persist auth</summary>
    public IBrowserContext Context => _context ?? throw new InvalidOperationException(
        "PlaywrightDriver has not been initialised.");

    /// <summary>
    /// Called from BrowserHooks.BeforeScenario. Creates a context (with optional StorageState)
    /// and a fresh page. Starts a Playwright trace if configured.
    /// </summary>
    public async Task InitialiseAsync(BrowserFactory factory, string? storageStatePath = null)
    {
        _context = await factory.CreateContextAsync(storageStatePath);

        if (_settings.RecordTrace)
        {
            await _context.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots   = true,
                Sources     = false
            });
            _traceStarted = true;
        }

        _page = await _context.NewPageAsync();

        // Route console errors to Serilog for debugging flaky tests
        _page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
                Log.Warning("[Browser Console Error] {Message}", msg.Text);
        };

        _page.PageError += (_, error) =>
            Log.Error("[Browser Page Error] {Error}", error);

        Log.Debug("PlaywrightDriver initialised - new page created");
    }

    /// <summary>
    /// Saves the Playwright trace to the reporting directory.
    /// Call this from AfterScenario hooks when a test fails.
    /// </summary>
    public async Task SaveTraceAsync(string scenarioTitle)
    {
        if (!_traceStarted || _context is null) return;

        var safeName = string.Concat(scenarioTitle.Split(Path.GetInvalidFileNameChars()));
        var tracePath = Path.Combine(_reporting.TraceDirectory, $"{safeName}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip");

        Directory.CreateDirectory(_reporting.TraceDirectory);

        await _context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
        _traceStarted = false;

        Log.Information("Playwright trace saved to {TracePath}", tracePath);
    }

    /// <summary>
    /// Stops trace without saving (for passing tests to avoid disk bloat).
    /// </summary>
    public async Task DiscardTraceAsync()
    {
        if (!_traceStarted || _context is null) return;
        await _context.Tracing.StopAsync(new TracingStopOptions());
        _traceStarted = false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_page is not null)
        {
            await _page.CloseAsync();
        }

        if (_context is not null)
        {
            if (_traceStarted)
                await _context.Tracing.StopAsync(new TracingStopOptions());

            await _context.CloseAsync();
            await _context.DisposeAsync();
        }

        Log.Debug("PlaywrightDriver disposed");
    }
}
