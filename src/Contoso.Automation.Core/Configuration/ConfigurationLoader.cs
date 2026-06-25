using Microsoft.Extensions.Configuration;

namespace Contoso.Automation.Core.Configuration;

/// <summary>
/// Loads and provides the root TestConfiguration. Precedence (highest wins):
/// Environment variables → appsettings.{env}.json → appsettings.json → defaults
///
/// Environment variables use __ as section separator:
///   D365__Password=secret  →  TestConfiguration.D365.Password = "secret"
///   AI__AnthropicApiKey=sk-ant-...  →  TestConfiguration.AI.AnthropicApiKey = "..."
///
/// This makes it trivial to inject secrets in Azure DevOps / Jenkins without
/// committing them to source control.
/// </summary>
public static class ConfigurationLoader
{
    private static TestConfiguration? _instance;
    private static readonly object _lock = new();

    public static TestConfiguration Load(string? environment = null)
    {
        if (_instance is not null) return _instance;

        lock (_lock)
        {
            if (_instance is not null) return _instance;

            var env = environment
                ?? Environment.GetEnvironmentVariable("TEST_ENVIRONMENT")
                ?? "DEV";

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()      // D365__Password, AI__AnthropicApiKey, etc.
                .Build();

            var testConfig = new TestConfiguration();
            config.Bind(testConfig);
            testConfig.Environment = env;

            // Validate mandatory settings to fail fast before test execution starts
            ValidateMandatorySettings(testConfig);

            _instance = testConfig;
            return _instance;
        }
    }

    /// <summary>Resets the singleton - for test isolation in unit tests of the framework itself</summary>
    internal static void Reset() => _instance = null;

    private static void ValidateMandatorySettings(TestConfiguration config)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(config.D365.BaseUrl))
            missing.Add("D365:BaseUrl");

        if (string.IsNullOrWhiteSpace(config.D365.Username))
            missing.Add("D365:Username");

        if (string.IsNullOrWhiteSpace(config.Dataverse.ApiBaseUrl))
            missing.Add("Dataverse:ApiBaseUrl");

        if (missing.Any())
        {
            throw new InvalidOperationException(
                $"Missing mandatory configuration values: {string.Join(", ", missing)}. " +
                "Check appsettings.json or set the corresponding environment variables.");
        }
    }
}
