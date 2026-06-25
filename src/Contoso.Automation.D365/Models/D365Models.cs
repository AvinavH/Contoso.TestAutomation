namespace Contoso.Automation.D365.Models;

/// <summary>
/// Domain model representing a D365 Account record.
/// Used as the transfer object between test data builders, API clients, and page objects.
/// All properties nullable - test builders only set what the scenario requires.
/// </summary>
public sealed class AccountModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public string? Industry { get; set; }   // Option set label
    public string? AnnualRevenue { get; set; }
    public string? Description { get; set; }
    public int? NumberOfEmployees { get; set; }
}

/// <summary>D365 Contact record model</summary>
public sealed class ContactModel
{
    public Guid? Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public string? ParentAccountName { get; set; }
    public Guid? ParentAccountId { get; set; }

    /// <summary>Full name as D365 would display it</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>D365 Opportunity record model</summary>
public sealed class OpportunityModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public Guid? AccountId { get; set; }
    public string? ContactName { get; set; }
    public Guid? ContactId { get; set; }
    public string? EstimatedValue { get; set; }   // String to preserve currency formatting
    public string? CloseDate { get; set; }         // Locale-formatted date string
    public string? Description { get; set; }
    public int? CloseProbability { get; set; }
}
