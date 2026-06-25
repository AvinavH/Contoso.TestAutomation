# D365 Test Automation Framework

A production-grade UI and API automation framework for **Microsoft Dynamics 365 Sales Hub**, built with Playwright, Reqnroll BDD, Dataverse API test data management, and Claude AI integration.

---

## Stack

| Concern | Technology |
|---|---|
| UI Automation | Playwright for .NET |
| BDD | **Reqnroll** 2.x + NUnit 3 |
| D365 Target | Dynamics 365 CE / Sales Hub (UCI v9.2) |
| Test Data Lifecycle | Dataverse Web API via ROPC OAuth |
| API Tests | RestSharp + NUnit (browser-free) |
| AI Test Data | Claude (`claude-sonnet-4-6`) via Anthropic SDK |
| Reporting | Allure (CI dashboard) + ExtentReports (HTML) |
| Logging | Serilog (console + JSON structured file) |
| Primary CI/CD | Azure DevOps (multi-stage YAML) |
| Secondary CI/CD | Jenkins (Declarative Pipeline) |
| Runtime | .NET 8 / C# 12 |

> **Why Reqnroll?** SpecFlow was deprecated when Tricentis discontinued the open-source edition. Reqnroll is the community-maintained open-source continuation — fully API-compatible, same Gherkin syntax, same `[Binding]` attributes. The only code change from SpecFlow is replacing `using TechTalk.SpecFlow;` with `using Reqnroll;`.

---

## Architecture

```
src/
├── Contoso.Automation.Core/       # Playwright driver, config, base classes, retry, logging
├── Contoso.Automation.D365/       # D365 page objects, components, StorageState auth
├── Contoso.Automation.API/        # Dataverse OData client, ROPC token acquisition
└── Contoso.Automation.AI/         # Claude AI service, test data generator agent

tests/
├── Contoso.Automation.BDD/        # Reqnroll features, step defs, hooks, dual reporting
└── Contoso.Automation.API.Tests/  # Standalone Dataverse CRUD tests (no browser)

pipeline/
├── azure-pipelines.yml            # Build → Tests → Quality Gate → Reports
├── azure-pipelines-pr.yml         # Smoke-only PR validation
└── Jenkinsfile                    # Jenkins Declarative (mirrors ADO)
```

### Key Design Decisions

| Pattern | Implementation |
|---|---|
| Page Object Model + Components | `D365GridComponent`, `D365FormComponent`, `D365LookupFieldComponent` are reused across all entity pages |
| Centralised Selectors | All D365 `data-id`/`aria-label` locators in `D365Selectors.cs` — one file to update per D365 release |
| StorageState Auth | Login runs once per 8-hour window; all subsequent tests load the saved browser session |
| API-driven test data | Dataverse API seeds preconditions and cleans up after every scenario, regardless of outcome |
| AI test data | Claude generates contextually realistic data; Bogus is the fallback when no API key is set |
| Polly retry | All D365 UI interactions retry 3× with exponential backoff + jitter |
| Dual reporting | Allure for CI pipeline dashboards; ExtentReports for stakeholder email attachments |

---

## Local Setup

### Prerequisites

```bash
# .NET 8 SDK
winget install Microsoft.DotNet.SDK.8      # Windows
brew install dotnet@8                       # macOS

# Playwright browser
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

### Configure credentials

Fill in `tests/Contoso.Automation.BDD/appsettings.json` — or use environment variables (recommended) or `dotnet user-secrets`:

```bash
cd tests/Contoso.Automation.BDD
dotnet user-secrets set "D365:Password"      "your-password"
dotnet user-secrets set "AI:AnthropicApiKey" "sk-ant-..."
```

Environment variable precedence (highest wins):

```
Environment variables  →  appsettings.{TEST_ENVIRONMENT}.json  →  appsettings.json
```

### Run tests

```bash
# Full suite
dotnet test Contoso.TestAutomation.sln

# Smoke tests only (~5 min)
dotnet test tests/Contoso.Automation.BDD/ --filter TestCategory=Smoke

# API tests only, no browser (~1 min)
dotnet test tests/Contoso.Automation.API.Tests/

# Single feature tag
dotnet test tests/Contoso.Automation.BDD/ --filter TestCategory=AccountCreation

# Headed browser for visual debugging
Browser__Headless=false dotnet test tests/Contoso.Automation.BDD/ --filter TestCategory=Smoke
```

---

## Environment Variables Reference

| Variable | Description |
|---|---|
| `D365__BaseUrl` | `https://yourorg.crm4.dynamics.com` |
| `D365__Username` | Automation service account UPN |
| `D365__Password` | **Secret** — never commit |
| `Dataverse__ApiBaseUrl` | `https://yourorg.api.crm4.dynamics.com/api/data/v9.2` |
| `Dataverse__TenantId` | Azure AD tenant GUID |
| `Dataverse__ClientId` | App Registration client ID |
| `AI__AnthropicApiKey` | Optional — tests degrade gracefully without it |
| `Browser__Headless` | `true` in CI, `false` for local debugging |
| `TEST_ENVIRONMENT` | `DEV` / `UAT` / `CI` — selects `appsettings.{env}.json` |

---

## CI/CD Pipeline (Azure DevOps)

```
┌───────────┐   ┌──────────────────────────┐   ┌──────────────┐   ┌────────────────┐
│ 1 · Build │──▶│ 2 · Tests (parallel)     │──▶│ 3 · Quality  │──▶│ 4 · Reports    │
│           │   │  BDD UI  │  API Tests    │   │    Gate      │   │  Allure HTML   │
│ restore   │   │ Playwright│  Dataverse   │   │  ≥80% pass   │   │  ExtentReport  │
│ build     │   │          │               │   │  rate req'd  │   │  Artifacts     │
│ pw install│   └──────────┴───────────────┘   └──────────────┘   └────────────────┘
└───────────┘
```

- Pipeline secrets injected as ADO variable groups — never in YAML
- Playwright traces + screenshots archived as artifacts on any failure
- PR pipeline (`azure-pipelines-pr.yml`) runs `@Smoke` only for fast feedback (~5 min)
- Quality gate PowerShell parses TRX results and fails the pipeline below 80% pass rate

---

## D365 Authentication

The framework uses **Playwright StorageState** to persist the D365 browser session:

1. On first run, it performs a Microsoft OIDC login (email → password → KMSI prompt)
2. The session is saved to `.auth/d365-state.json` (git-ignored)
3. Subsequent test scenarios load that file — login is skipped
4. State expires after 8 hours (configurable via `D365:StorageStateExpiryHours`)

**Requirement:** The automation service account must have MFA disabled or excluded via Conditional Access policy.

The same account credentials drive both UI login (StorageState) and Dataverse API calls (ROPC OAuth via MSAL).

---

## Reporting

| Report | Path | Use case |
|---|---|---|
| ExtentReports | `reports/extent-report.html` | Stakeholder email/share |
| Allure results | `allure-results/` | `allure serve allure-results` |
| Playwright trace | `reports/traces/*.zip` | `playwright show-trace trace.zip` |
| Screenshots | `reports/screenshots/` | Failure evidence |
| Structured log | `reports/logs/*.log` | JSON per-step log |

---

## Extending the Framework

### Add a new D365 entity

```csharp
// src/Contoso.Automation.D365/Pages/Sales/MyEntityFormPage.cs
public sealed class MyEntityFormPage : BasePage
{
    private readonly D365FormComponent _form;
    public MyEntityFormPage(IPage page, TestConfiguration config) : base(page, config)
        => _form = new D365FormComponent(page, config);

    public async Task SetNameAsync(string name) => await _form.SetTextFieldAsync("name", name);
    public async Task SaveAsync()               => await _form.SaveAsync();
    public string GetRecordId()                 => ExtractRecordIdFromUrl();
}
```

### Add a new feature

```gherkin
@MyEntity @Smoke
Feature: My entity management
  Scenario: Create a record
    Given I am authenticated in D365 as a sales representative
    When I create a new my-entity named "Test Record"
    Then it should be saved successfully
```

Step definitions are auto-discovered by Reqnroll — no registration required.

---

## Reqnroll Migration Note

If you are familiar with SpecFlow, the migration is minimal:

| SpecFlow | Reqnroll |
|---|---|
| `using TechTalk.SpecFlow;` | `using Reqnroll;` |
| `using BoDi;` | `using Reqnroll.BoDi;` |
| `SpecFlow.NUnit` package | `Reqnroll.NUnit` package |
| `Allure.SpecFlow` package | `Allure.Reqnroll` package |
| `specflow.json` | `reqnroll.json` |
| Everything else | Identical |

---

*Built with Playwright, Reqnroll, and Claude · D365 Test Automation Framework*
