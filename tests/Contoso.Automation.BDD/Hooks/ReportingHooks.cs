using Allure.Net.Commons;
using Contoso.Automation.BDD.Support;
using Contoso.Automation.Core.Configuration;
using Contoso.Automation.Core.Drivers;
using Contoso.Automation.Core.Helpers;
using Microsoft.Playwright;
using Reqnroll;
using Serilog;

namespace Contoso.Automation.BDD.Hooks;

/// <summary>
/// Reporting hooks using Allure (CI dashboard) for scenario-level reporting.
///
/// Allure is largely automatic when Allure.Reqnroll is referenced — the plugin
/// intercepts Reqnroll's step events and writes allure-results/*.json files.
/// We supplement it here with failure screenshots attached as Allure attachments,
/// and custom suite/story labels for better grouping in the Allure UI.
///
/// Reqnroll note: [BeforeTestRun], [AfterTestRun], [BeforeScenario], [AfterScenario],
/// and [AfterStep] behave identically to SpecFlow — only the using statement changes.
/// </summary>
[Binding]
public sealed class ReportingHooks
{
    private readonly ScenarioContext   _scenarioContext;
    private readonly CrmTestContext    _ctx;
    private readonly PlaywrightDriver  _driver;
    private readonly ReportingSettings _reporting;
    private readonly ILogger           _log = Log.ForContext<ReportingHooks>();

    public ReportingHooks(
        ScenarioContext scenarioContext,
        CrmTestContext ctx,
        PlaywrightDriver driver,
        TestConfiguration config)
    {
        _scenarioContext = scenarioContext;
        _ctx             = ctx;
        _driver          = driver;
        _reporting       = config.Reporting;
    }

    // ─── Test Run ─────────────────────────────────────────────────────────────

    [BeforeTestRun]
    public static void InitialiseReporting()
    {
        TestLogger.Configure();
        Log.Information("Reporting initialised");
    }

    [AfterTestRun]
    public static void FlushReporting()
    {
        TestLogger.CloseAndFlush();
    }

    // ─── Scenario ─────────────────────────────────────────────────────────────

    [BeforeScenario(Order = 10)]
    public void BeforeScenario()
    {
        var info = _scenarioContext.ScenarioInfo;

        AllureApi.SetSuiteName(info.Title);
        AllureApi.SetStoryName(info.Title);
        foreach (var tag in info.Tags)
            AllureApi.AddLabel("tag", tag);

        _log.Information("▶ Scenario: {Title}", info.Title);
    }

    [AfterScenario(Order = 200)]
    public async Task AfterScenarioAsync()
    {
        var error = _scenarioContext.TestError;

        if (error is not null)
        {
            var shot = await TryScreenshotAsync();
            if (shot is not null)
            {
                var bytes = await File.ReadAllBytesAsync(shot);
                AllureApi.AddAttachment("Failure Screenshot", "image/png", bytes, ".png");
            }
            _log.Warning("✗ FAILED: {Title} — {Error}", _scenarioContext.ScenarioInfo.Title, error.Message);
        }
        else
        {
            _log.Information("✓ PASSED: {Title}", _scenarioContext.ScenarioInfo.Title);
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string?> TryScreenshotAsync()
    {
        if (!_reporting.TakeScreenshotOnFailure) return null;
        try
        {
            Directory.CreateDirectory(_reporting.ScreenshotDirectory);
            var safe = string.Concat(
                _scenarioContext.ScenarioInfo.Title
                    .Split(Path.GetInvalidFileNameChars()))
                .Replace(" ", "_");
            var path = Path.Combine(_reporting.ScreenshotDirectory,
                $"{safe}_{DateTime.UtcNow:yyyyMMddHHmmss}.png");
            await _driver.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path });
            return path;
        }
        catch { return null; }
    }
}
