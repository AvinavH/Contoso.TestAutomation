using Serilog;
using Serilog.Events;

namespace Contoso.Automation.Core.Helpers;

/// <summary>
/// Configures Serilog for the test run. Called once from BeforeTestRun hook.
/// Logs flow to:
///   - Console (coloured, for local debugging)
///   - File (JSON structured, for CI log ingestion)
///
/// Each log entry carries scenario context properties injected by ReportingHooks,
/// making it trivial to filter logs for a specific test in CI dashboards.
/// </summary>
public static class TestLogger
{
    public static void Configure(string logDirectory = "reports/logs")
    {
        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, $"test-run_{DateTime.UtcNow:yyyyMMdd_HHmmss}.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ThreadId}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information)
            .WriteTo.File(
                logPath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{ThreadId}] {Scenario} {Message:lj}{NewLine}{Exception}",
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 50 * 1024 * 1024)
            .CreateLogger();

        Log.Information("Test logger configured. Log file: {Path}", logPath);
    }

    public static void CloseAndFlush() => Log.CloseAndFlush();
}
