using Newtonsoft.Json;
using Contoso.Automation.AI.Services;
using Contoso.Automation.D365.Models;
using Serilog;

namespace Contoso.Automation.AI.Agents;

/// <summary>
/// Uses Claude to generate contextually realistic D365 test data.
/// Falls back to Bogus-generated data when AI is unavailable, ensuring
/// tests always run even without an API key.
///
/// AI-generated data is superior to Bogus for D365 portfolio demos because:
/// - Account names sound like plausible financial services companies
/// - Opportunity descriptions match relevant domain language
/// - Data passes D365 field-level validation rules
/// - Each scenario gets unique, non-repeating data
/// </summary>
public sealed class TestDataGeneratorAgent
{
    private readonly ClaudeAIService _ai;
    private readonly ILogger _log = Log.ForContext<TestDataGeneratorAgent>();

    // Bogus Faker instances for AI fallback
    private static readonly Bogus.Faker Faker = new Bogus.Faker("en_GB");

    public TestDataGeneratorAgent(ClaudeAIService ai) => _ai = ai;

    /// <summary>
    /// Generates a realistic AccountModel. When AI is available, the data reflects
    /// a plausible UK financial services or infrastructure company matching the target domain.
    /// </summary>
    public async Task<AccountModel> GenerateAccountAsync(string? context = null)
    {
        if (!_ai.IsEnabled)
            return GenerateFallbackAccount();

        var prompt = $$"""
            Generate test data for a D365 Account record for a UK financial services or infrastructure company
            that might be an investment target for the the organisation.
            {{(context is not null ? $"Additional context: {context}" : string.Empty)}}

            Return a JSON object with these fields:
            {
              "Name": "company name (realistic UK business name)",
              "Phone": "UK phone number in +44 format",
              "Website": "www.companyname.co.uk",
              "Email": "contact@companyname.co.uk",
              "City": "a UK city",
              "Industry": one of "Financial Services", "Energy", "Infrastructure", "Technology",
              "AnnualRevenue": "integer in millions as string e.g. '250000000'"
            }
            """;

        var json = await _ai.CompleteAsJsonAsync(prompt,
            "You are a test data generator for UK financial services automation testing. Generate realistic but fictional data.");

        if (json is null) return GenerateFallbackAccount();

        try
        {
            var account = JsonConvert.DeserializeObject<AccountModel>(json);
            _log.Debug("AI-generated Account: {Name}", account?.Name);
            return account ?? GenerateFallbackAccount();
        }
        catch (JsonException)
        {
            _log.Warning("Could not parse AI account data - using fallback");
            return GenerateFallbackAccount();
        }
    }

    /// <summary>Generates a realistic ContactModel for a D365 Contact record</summary>
    public async Task<ContactModel> GenerateContactAsync(string? accountContext = null)
    {
        if (!_ai.IsEnabled)
            return GenerateFallbackContact();

        var prompt = $$"""
            Generate test data for a D365 Contact record for a senior professional at a UK financial firm.
            {{(accountContext is not null ? $"Context: {accountContext}" : string.Empty)}}

            Return JSON:
            {
              "FirstName": "realistic UK first name",
              "LastName":  "realistic UK surname",
              "Email":     "firstname.lastname@company.co.uk",
              "Phone":     "+44 format phone",
              "JobTitle":  "a senior finance or investment role"
            }
            """;

        var json = await _ai.CompleteAsJsonAsync(prompt,
            "You are a test data generator. Generate realistic but entirely fictional personal data.");

        if (json is null) return GenerateFallbackContact();

        try
        {
            return JsonConvert.DeserializeObject<ContactModel>(json) ?? GenerateFallbackContact();
        }
        catch
        {
            return GenerateFallbackContact();
        }
    }

    /// <summary>Generates a realistic OpportunityModel representing a business opportunity</summary>
    public async Task<OpportunityModel> GenerateOpportunityAsync()
    {
        if (!_ai.IsEnabled)
            return GenerateFallbackOpportunity();

        var prompt = """
            Generate test data for a D365 Opportunity record representing a the organisation investment.
            Return JSON:
            {
              "Name":           "opportunity name (e.g. 'Series B - Green Energy Ltd')",
              "EstimatedValue": "value in pounds as integer string e.g. '50000000'",
              "CloseDate":      "date in dd/MM/yyyy format, 6-18 months from today",
              "Description":    "2-3 sentence investment rationale"
            }
            """;

        var json = await _ai.CompleteAsJsonAsync(prompt,
            "Generate test data for a UK sovereign wealth fund investment automation framework.");

        if (json is null) return GenerateFallbackOpportunity();

        try
        {
            return JsonConvert.DeserializeObject<OpportunityModel>(json) ?? GenerateFallbackOpportunity();
        }
        catch
        {
            return GenerateFallbackOpportunity();
        }
    }

    // ─── Bogus fallbacks ───────────────────────────────────────────────────────

    private static AccountModel GenerateFallbackAccount() => new()
    {
        Name          = $"{Faker.Company.CompanyName()} Ltd",
        Phone         = $"+44 {Faker.Phone.PhoneNumber("2# #### ####")}",
        Website       = $"www.{Faker.Internet.DomainName()}",
        Email         = Faker.Internet.Email(),
        City          = Faker.PickRandom("London", "Manchester", "Leeds", "Birmingham", "Edinburgh"),
        Industry      = Faker.PickRandom("Financial Services", "Energy", "Infrastructure", "Technology"),
        AnnualRevenue = Faker.Random.Int(10_000_000, 500_000_000).ToString()
    };

    private static ContactModel GenerateFallbackContact() => new()
    {
        FirstName = Faker.Name.FirstName(),
        LastName  = Faker.Name.LastName(),
        Email     = Faker.Internet.Email(),
        Phone     = $"+44 {Faker.Phone.PhoneNumber("7### ######")}",
        JobTitle  = Faker.PickRandom("CFO", "Investment Director", "Head of Finance", "Portfolio Manager")
    };

    private static OpportunityModel GenerateFallbackOpportunity() => new()
    {
        Name           = $"Investment - {Faker.Company.CompanyName()}",
        EstimatedValue = Faker.Random.Int(5_000_000, 100_000_000).ToString(),
        CloseDate      = DateTime.Today.AddMonths(Faker.Random.Int(6, 18)).ToString("dd/MM/yyyy"),
        Description    = Faker.Lorem.Sentences(2)
    };
}
