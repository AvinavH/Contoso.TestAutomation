@Accounts
Feature: Account creation in D365 Sales Hub
  As a sales representative
  I want to create Account records in D365
  So that I can track customers and investment targets

  Background:
    Given I am authenticated in D365 as a sales representative
    And I navigate to the Accounts module

  @Smoke @AccountCreation
  Scenario: Create a new account with all mandatory fields
    When I click New to open the Account form
    And I fill in the account name "Automation Test Corp Ltd"
    And I fill in the business phone "+44 207 946 0958"
    And I fill in the website "www.autotestcorp.co.uk"
    And I save the account
    Then the account should be saved without errors
    And the form title should display "Automation Test Corp Ltd"
    And the account "Automation Test Corp Ltd" should appear in the accounts list

  @Regression @AccountCreation @Validation
  Scenario: Mandatory account name field is enforced on save
    When I click New to open the Account form
    And I attempt to save the account without entering a name
    Then the account name field should show a mandatory validation error
    And the account should not be saved

  @Regression @AccountCreation
  Scenario: Create an account using AI-generated test data
    When I create a new account using AI-generated data
    Then the account should be saved without errors
    And the saved account should appear in the accounts grid

  @Regression @AccountCreation
  Scenario Outline: Create accounts for different industry sectors
    When I click New to open the Account form
    And I fill in the account name "<AccountName>"
    And I select "<Industry>" as the industry
    And I save the account
    Then the account "<AccountName>" should appear in the accounts list

    Examples:
      | AccountName                  | Industry           |
      | Test Energy Partners Ltd     | Energy             |
      | Test Infrastructure Holdings | Infrastructure     |
      | Test Finance Corp Ltd        | Financial Services |

  @Smoke @AccountCreation @API
  Scenario: Verify account data is correctly persisted in Dataverse
    When I create a new account with the following details:
      | Field   | Value                   |
      | Name    | API Verification Co Ltd |
      | Phone   | +44 113 496 0123        |
      | Website | www.apiverifycorp.co.uk |
    And I save the account
    Then the account record in Dataverse should have:
      | Field      | Value                   |
      | name       | API Verification Co Ltd |
      | telephone1 | +44 113 496 0123        |
