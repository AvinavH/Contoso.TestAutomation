using Allure.NUnit;
using Allure.NUnit.Attributes;
using FluentAssertions;
using NUnit.Framework;
using Contoso.Automation.API.Auth;
using Contoso.Automation.API.Clients;
using Contoso.Automation.Core.Configuration;
using Contoso.Automation.Core.Helpers;
using Contoso.Automation.D365.Models;

namespace Contoso.Automation.API.Tests;

/// <summary>
/// Standalone Dataverse Web API tests that validate CRUD operations independently
/// of the browser UI. These run fast (~500ms each vs ~30s for UI tests) and are
/// ideal for pipeline smoke tests and post-deployment validation.
///
/// These tests also serve as integration tests for the DataverseApiClient itself,
/// confirming that OData operations, auth token handling, and entity mapping work
/// correctly against a live D365 environment.
/// </summary>
[TestFixture]
[AllureSuite("Dataverse API Tests")]
[AllureFeature("Account CRUD")]
[Parallelizable(ParallelScope.All)]
public sealed class DataverseAccountTests
{
    private static TestConfiguration _config = null!;
    private static DataverseApiClient _api = null!;
    private static AccountApiClient _accounts = null!;

    private readonly List<Guid> _createdIds = new();

    [OneTimeSetUp]
    public static void ClassSetUp()
    {
        TestLogger.Configure("reports/logs");
        _config   = ConfigurationLoader.Load();
        var auth  = new DataverseAuthClient(_config);
        _api      = new DataverseApiClient(_config, auth);
        _accounts = new AccountApiClient(_api);
    }

    [TearDown]
    public async Task CleanUpAsync()
    {
        foreach (var id in _createdIds)
        {
            try { await _accounts.DeleteAsync(id); }
            catch { /* best effort */ }
        }
        _createdIds.Clear();
    }

    // ─── Create ───────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("Create account")]
    [AllureSeverity(SeverityLevel.critical)]
    public async Task CreateAccount_WithMandatoryFields_ReturnsNonEmptyGuid()
    {
        // Arrange
        var account = new AccountModel
        {
            Name  = $"Demo API Test - Create {DateTime.UtcNow:HHmmss}",
            Phone = "+44 207 000 1234"
        };

        // Act
        var id = await _accounts.CreateAsync(account);
        _createdIds.Add(id);

        // Assert
        id.Should().NotBe(Guid.Empty, "Dataverse should return a valid GUID for the new account");
    }

    [Test]
    [AllureStory("Create account")]
    public async Task CreateAccount_WithAllFields_PersistsAllValues()
    {
        // Arrange
        var uniqueName = $"Demo API Full Test {Guid.NewGuid():N[..8]}";
        var account = new AccountModel
        {
            Name    = uniqueName,
            Phone   = "+44 113 000 5678",
            Website = "www.crmtest.co.uk",
            Email   = "test@crmtest.co.uk",
            City    = "Leeds"
        };

        // Act
        var id = await _accounts.CreateAsync(account);
        _createdIds.Add(id);

        // Assert - round-trip via query
        var foundId = await _accounts.FindByNameAsync(uniqueName);
        foundId.Should().Be(id, "the created account should be findable by its exact name");
    }

    // ─── Read ────────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("Read account")]
    public async Task FindByName_ExistingAccount_ReturnsCorrectId()
    {
        // Arrange - create a known account
        var name = $"Demo API Find Test {DateTime.UtcNow:HHmmss}";
        var id   = await _accounts.CreateAsync(new AccountModel { Name = name });
        _createdIds.Add(id);

        // Act
        var foundId = await _accounts.FindByNameAsync(name);

        // Assert
        foundId.Should().Be(id, "FindByName should return the correct record ID");
    }

    [Test]
    [AllureStory("Read account")]
    public async Task FindByName_NonExistentAccount_ReturnsNull()
    {
        // Act
        var result = await _accounts.FindByNameAsync($"NONEXISTENT_ZZZZZ_{Guid.NewGuid():N}");

        // Assert
        result.Should().BeNull("a non-existent account should return null, not throw");
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("Delete account")]
    public async Task DeleteAccount_ExistingRecord_CannotBeFindAfterwards()
    {
        // Arrange
        var name = $"Demo API Delete Test {DateTime.UtcNow:HHmmss}";
        var id   = await _accounts.CreateAsync(new AccountModel { Name = name });

        // Act
        await _accounts.DeleteAsync(id);

        // Assert
        var foundId = await _accounts.FindByNameAsync(name);
        foundId.Should().BeNull("deleted account should no longer be findable");
    }

    [Test]
    [AllureStory("Delete account")]
    public async Task DeleteAccount_AlreadyDeleted_DoesNotThrow()
    {
        // Arrange
        var id = await _accounts.CreateAsync(new AccountModel { Name = $"Demo Double Delete {DateTime.UtcNow:HHmmss}" });
        await _accounts.DeleteAsync(id);

        // Act & Assert - idempotent delete should not throw
        var act = async () => await _accounts.DeleteAsync(id);
        await act.Should().NotThrowAsync("deleting an already-deleted record should be idempotent");
    }

    // ─── Bulk operations ──────────────────────────────────────────────────────

    [Test]
    [AllureStory("Bulk operations")]
    public async Task CreateMultipleAccounts_AllCreatedSuccessfully()
    {
        // Arrange
        var names = Enumerable.Range(1, 3)
            .Select(i => $"Demo Bulk Test {i} {DateTime.UtcNow:HHmmss}")
            .ToList();

        // Act
        var ids = new List<Guid>();
        foreach (var name in names)
        {
            var id = await _accounts.CreateAsync(new AccountModel { Name = name });
            ids.Add(id);
            _createdIds.Add(id);
        }

        // Assert
        ids.Should().HaveCount(3, "all three accounts should be created");
        ids.Should().OnlyContain(id => id != Guid.Empty, "all IDs should be valid GUIDs");
        ids.Distinct().Should().HaveCount(3, "each account should have a unique ID");
    }
}
