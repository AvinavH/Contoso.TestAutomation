using Allure.Net.Commons;
using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using Contoso.Automation.BDD.Support;
using Contoso.Automation.Core.Configuration;
using Contoso.Automation.Core.Drivers;
using Contoso.Automation.Core.Helpers;
using Microsoft.Playwright;
using Reqnroll;
using Serilog;

namespace Contoso.Automation.BDD.Hooks;

/// <summary>
/// Dual reporting stack: Allure (CI dashboard) + ExtentReports (stakeholder HTML).
///
/// Allure is largely automatic when Allure.Reqnroll is referenced — the plugin
/// intercepts Reqnroll's step events and writes allure-results/*.json files.
/// We supplement it here with failure screenshots attached as Allure attachments,
/// and custom suite/story labels for better grouping in the Allure UI.
///
/// ExtentReports is manually driven: we create a test node per scenario,
/// log each step's outcome, and attach failure screenshots.
///
/// Reqnroll note: [BeforeTestRun], [AfterTestRun], [BeforeScenario], [AfterScenario],
/// and [AfterStep] behave identically to SpecFlow — only the using statement changes.
/// </summary>
[Binding]
public sealed class ReportingHooks
{
    private static ExtentReports? _extent;
    private static readonly Lock ExtentLock = new();

    [ThreadStatic]
    private static ExtentTest? _currentTest;

    private readonly ScenarioContext    _scenarioContext;
    private readonly CrmTestContext     _ctx;
    private readonly PlaywrightDriver   _driver;
    private readonly ReportingSettings  _reporting;
    private readonly ILogger            _log = Log.ForContext<ReportingHooks>();

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

        lock (ExtentLock)
        {
            Directory.CreateDirectory("reports");

            var spark = new ExtentSparkReporter(_reporting_path())
            {
                Config =
                {
                    Theme         = AventStack.ExtentReports.Reporter.Config.Theme.Dark,
                    DocumentTitle = "D365 Automation Report",
                    ReportName    = "D365 Test Automation Results",
                    Encoding      = "UTF-8"
                }
            };

            _extent = new ExtentReports();
            _extent.AttachReporter(spark);
            _extent.AddSystemInfo("Framework", "Playwright + Reqnroll + .NET 8");
            _extent.AddSystemInfo("Environment",
                Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") ?? "DEV");
        }

        Log.Information("Reporting initialised");
    }

    [AfterTestRun]
    public static void FlushReporting()
    {
        lock (ExtentLock) { _extent?.Flush(); }
        TestLogger.CloseAndFlush();
    }

    // ─── Scenario ─────────────────────────────────────────────────────────────

    [BeforeScenario(Order = 10)]
    public void BeforeScenario()
    {
        var info = _scenarioContext.ScenarioInfo;
        lock (ExtentLock)
        {
            _currentTest = _extent?.CreateTest(
                info.Title,
                $"Tags: {string.Join(", ", info.Tags)}");
        }

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
            _currentTest?.Fail(error.Message);
            if (shot is not null)
            {
                _currentTest?.AddScreenCaptureFromPath(shot, "Failure Screenshot");
                var bytes = await File.ReadAllBytesAsync(shot);
                AllureApi.AddAttachment("Failure Screenshot", "image/png", bytes, ".png");
            }
            _log.Warning("✗ FAILED: {Title} — {Error}", _scenarioContext.ScenarioInfo.Title, error.Message);
        }
        else
        {
            _currentTest?.Pass("Passed");
            _log.Information("✓ PASSED: {Title}", _scenarioContext.ScenarioInfo.Title);
        }
    }

    [AfterStep]
    public void AfterStep()
    {
        var step = _scenarioContext.StepContext.StepInfo;
        var text = $"{step.StepDefinitionType} {step.Text}";

        if (_scenarioContext.TestError is null)
            _currentTest?.Log(Status.Pass, text);
        else
            _currentTest?.Log(Status.Fail, $"{text} | {_scenarioContext.TestError.Message}");
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

    // Static helper to avoid accessing instance fields in static method
    private static string _reporting_path() => "reports/extent-report.html";
}
