@Opportunities
Feature: Opportunity management in D365 Sales Hub
  As a sales representative
  I want to create and track Opportunities in D365
  So that I can manage the full sales pipeline lifecycle

  Background:
    Given I am authenticated in D365 as a sales representative

  @Smoke @OpportunityCreation
  Scenario: Create a new opportunity with mandatory fields
    Given I navigate to the Opportunities module
    When I click New to open the Opportunity form
    And I fill in the opportunity name "Series B — Test Clean Energy 2025"
    And I set the estimated value to "25000000"
    And I set the close date to "31/12/2025"
    And I save the opportunity
    Then the opportunity should be saved without errors
    And the form should display "Series B — Test Clean Energy 2025"

  @Regression @OpportunityCreation
  Scenario: Create an opportunity linked to an account and contact
    Given an account "Test Infrastructure Holdings" exists in the system
    And a contact "Robert Clarke-Auto" linked to "Test Infrastructure Holdings" exists
    And I navigate to the Opportunities module
    When I click New to open the Opportunity form
    And I fill in the opportunity name "Infrastructure Bond Q1 2025"
    And I link the opportunity to account "Test Infrastructure Holdings"
    And I link the opportunity to contact "Robert Clarke-Auto"
    And I set the estimated value to "50000000"
    And I set the close date to "31/03/2025"
    And I save the opportunity
    Then the opportunity should be saved without errors
    And the opportunity should be linked to account "Test Infrastructure Holdings"

  @Regression @OpportunityCreation
  Scenario: Create an opportunity using AI-generated data
    Given I navigate to the Opportunities module
    When I create a new opportunity using AI-generated data
    Then the opportunity should be saved without errors

  @Smoke @OpportunitySearch
  Scenario: Search for an opportunity by name
    Given an opportunity "Findable Deal 2025 Unique" exists in the system
    And I navigate to the Opportunities module
    When I search for "Findable Deal 2025"
    Then "Findable Deal 2025 Unique" should be visible in the opportunities grid

  @Regression @OpportunityValidation
  Scenario: Opportunity name is a mandatory field
    Given I navigate to the Opportunities module
    When I click New to open the Opportunity form
    And I attempt to save the opportunity without a name
    Then the opportunity name field should show a mandatory validation error

  @Regression @OpportunityWorkflow
  Scenario: Opportunity data is correctly stored in Dataverse
    When I create a new opportunity with the following details:
      | Field          | Value               |
      | Name           | API Data Check Q4   |
      | EstimatedValue | 10000000            |
    And I save the opportunity
    Then the opportunity record in Dataverse should have:
      | Field | Value             |
      | name  | API Data Check Q4 |
