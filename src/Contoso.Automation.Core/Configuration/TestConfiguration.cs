namespace Contoso.Automation.Core.Configuration;

/// <summary>
/// Root configuration model. Binds from appsettings.json, environment variables,
/// and user secrets. Environment variables use double-underscore notation
/// (e.g. D365__Password maps to D365.Password) for CI/CD pipeline secrets.
/// </summary>
public sealed class TestConfiguration
{
    public D365Settings D365 { get; set; } = new();
    public BrowserSettings Browser { get; set; } = new();
    public DataverseSettings Dataverse { get; set; } = new();
    public AISettings AI { get; set; } = new();
    public ReportingSettings Reporting { get; set; } = new();
    public string Environment { get; set; } = "DEV";
}

public sealed class D365Settings
{
    /// <summary>Root D365 org URL, e.g. https://orga3bb73ea.crm11.dynamics.com (no path)</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Full app entry URL including appid, e.g. https://org.crm11.dynamics.com/main.aspx?appid=xxx
    /// Used for initial navigation. Falls back to BaseUrl when empty.
    /// </summary>
    public string AppUrl { get; set; } = string.Empty;

    /// <summary>Automation service account UPN</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Populated from D365__Password environment variable or user secrets - never committed</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Path to Playwright StorageState JSON file persisting the browser session</summary>
    public string StorageStatePath { get; set; } = ".auth/d365-state.json";

    /// <summary>Hours before a stored session is considered stale and re-login is triggered</summary>
    public int StorageStateExpiryHours { get; set; } = 8;

    /// <summary>App module name in the D365 app switcher, e.g. "Sales Hub"</summary>
    public string AppName { get; set; } = "Sales Hub";
}

public sealed class BrowserSettings
{
    public string BrowserType { get; set; } = "chromium";

    /// <summary>Set false locally for visual debugging; always true in CI</summary>
    public bool Headless { get; set; } = false;

    /// <summary>Global Playwright timeout for element interactions (ms)</summary>
    public int DefaultTimeoutMs { get; set; } = 30_000;

    /// <summary>Extended timeout for D365 page navigations (ms) - UCI is notoriously slow</summary>
    public int NavigationTimeoutMs { get; set; } = 60_000;

    /// <summary>Slow down Playwright actions by this many ms; useful for local debugging</summary>
    public float SlowMo { get; set; } = 0;

    /// <summary>Record video for every test - expensive; set true only in CI debug runs</summary>
    public bool RecordVideo { get; set; } = false;

    /// <summary>Playwright trace (network, screenshots, DOM snapshots) - attach on failure</summary>
    public bool RecordTrace { get; set; } = true;

    public int ViewportWidth { get; set; } = 1920;
    public int ViewportHeight { get; set; } = 1080;
}

public sealed class DataverseSettings
{
    /// <summary>Dataverse OData endpoint, e.g. https://nwforg.api.crm4.dynamics.com/api/data/v9.2</summary>
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>Azure AD tenant ID for ROPC token acquisition</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Azure AD App Registration client ID with Dynamics 365 user_impersonation scope</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>D365 resource URI for scope binding, e.g. https://nwforg.crm4.dynamics.com/</summary>
    public string Resource { get; set; } = string.Empty;
}

public sealed class AISettings
{
    /// <summary>Anthropic API key - populated from AI__AnthropicApiKey environment variable</summary>
    public string AnthropicApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "claude-sonnet-4-6";

    /// <summary>When false, AI-dependent features degrade gracefully without failing tests</summary>
    public bool Enabled { get; set; } = true;

    public int MaxTokens { get; set; } = 1024;
}

public sealed class ReportingSettings
{
    public string AllureResultsDirectory { get; set; } = "allure-results";
    public string ExtentReportPath { get; set; } = "reports/extent-report.html";
    public string ScreenshotDirectory { get; set; } = "reports/screenshots";
    public string TraceDirectory { get; set; } = "reports/traces";
    public string VideoDirectory { get; set; } = "reports/videos";
    public bool TakeScreenshotOnFailure { get; set; } = true;
    public bool AttachTraceOnFailure { get; set; } = true;
}
