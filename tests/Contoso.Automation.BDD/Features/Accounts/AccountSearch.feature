@Accounts @Search
Feature: Account search and grid interactions
  As a sales representative
  I want to search for and navigate accounts in the D365 grid
  So that I can quickly locate records across large datasets

  Background:
    Given I am authenticated in D365 as a sales representative
    And the following accounts exist in the system:
      | Name                       |
      | Search Target Alpha Corp   |
      | Search Target Beta Corp    |

  @Smoke @Search
  Scenario: Search returns matching accounts
    Given I navigate to the Accounts module
    When I search for "Search Target Alpha"
    Then the grid should display at least 1 result
    And "Search Target Alpha Corp" should be visible in the grid

  @Regression @Search
  Scenario: Search with no matching results shows empty grid
    Given I navigate to the Accounts module
    When I search for "ZZZNORESULT999XYZ"
    Then the grid should display 0 results

  @Smoke @Search
  Scenario: Opening a record from the grid navigates to the form
    Given I navigate to the Accounts module
    When I search for "Search Target Beta"
    And I open the record "Search Target Beta Corp"
    Then I should be on the Account form for "Search Target Beta Corp"

  @Regression @Search
  Scenario: Newly created account is immediately searchable
    Given I navigate to the Accounts module
    When I create a new account named "Freshly Created Corp 99"
    And I save and close the account
    And I navigate back to the Accounts module
    And I search for "Freshly Created Corp 99"
    Then "Freshly Created Corp 99" should be visible in the grid
