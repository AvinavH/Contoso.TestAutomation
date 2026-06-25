@Contacts
Feature: Contact management in D365 Sales Hub
  As a sales representative
  I want to create and link Contact records in D365
  So that I can track key people at each account

  Background:
    Given I am authenticated in D365 as a sales representative

  @Smoke @ContactCreation
  Scenario: Create a new contact with mandatory fields
    Given I navigate to the Contacts module
    When I click New to open the Contact form
    And I fill in the first name "James"
    And I fill in the last name "Thornton-Auto"
    And I fill in the job title "Chief Investment Officer"
    And I fill in the email "j.thornton@autotest.co.uk"
    And I save the contact
    Then the contact should be saved without errors
    And the form should display "James Thornton-Auto"

  @Regression @ContactCreation
  Scenario: Create a contact linked to an existing account
    Given an account "Parent Corp Ltd" exists in the system
    And I navigate to the Contacts module
    When I click New to open the Contact form
    And I fill in the first name "Sarah"
    And I fill in the last name "Pemberton-Auto"
    And I link the contact to account "Parent Corp Ltd"
    And I save the contact
    Then the contact should be saved without errors

  @Regression @ContactCreation
  Scenario: Create a contact using AI-generated data
    Given I navigate to the Contacts module
    When I create a new contact using AI-generated data
    Then the contact should be saved without errors
    And the saved contact should appear in the contacts grid

  @Smoke @ContactSearch
  Scenario: Search for a contact by last name
    Given a contact "Alexandra Whitfield-Auto" exists in the system
    And I navigate to the Contacts module
    When I search for "Whitfield-Auto"
    Then "Alexandra Whitfield-Auto" should be visible in the contacts grid

  @Regression @ContactValidation
  Scenario: Last name is a mandatory field
    Given I navigate to the Contacts module
    When I click New to open the Contact form
    And I fill in the first name "NoSurnameTest"
    And I attempt to save the contact without a last name
    Then the last name field should show a mandatory validation error
