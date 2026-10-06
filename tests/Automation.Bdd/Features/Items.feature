@Sample @NoBrowser
Feature: API test data lifecycle
  Scenario: Create an item through the generated client
    Given an isolated application
    When I create an item named "BDD example"
    Then the item can be read from the API
