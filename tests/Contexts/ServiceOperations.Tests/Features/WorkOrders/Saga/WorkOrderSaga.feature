@work-order-saga
Feature: Work order orchestration
  The work order coordinates budget, inventory, payment and execution.

  Background:
    Given a diagnosed work order with a requested service and part
    And its budget has been issued

  Scenario: Successful work order
    When the budget is approved
    And the parts are reserved
    And payment is confirmed
    And execution is completed
    Then the work order is Completed
    And reservation, payment and execution commands were published

  Scenario: Budget rejection returns the work order to diagnosis
    When the budget is rejected
    Then the work order is InDiagnosis
    And no compensation commands were published

  Scenario: Approval timeout cancels the budget
    When approval times out
    Then the work order is Cancelled
    And the budget was cancelled

  Scenario: Inventory failure cancels the budget and returns to diagnosis
    When the budget is approved
    And parts reservation fails
    Then the work order is InDiagnosis
    And the budget was cancelled

  Scenario Outline: Payment failure or timeout releases reserved parts and cancels the budget
    When the budget is approved
    And the parts are reserved
    And <failure>
    Then the work order is Cancelled
    And the budget was cancelled
    And the parts were released
    And no refund was requested

    Examples:
      | failure           |
      | payment fails     |
      | payment times out |

  Scenario: Execution failure refunds payment and releases parts
    When the budget is approved
    And the parts are reserved
    And payment is confirmed
    And execution fails
    Then the work order is Cancelled
    And the parts were released
    And a refund was requested

  Scenario: Duplicate payment confirmation is ignored
    When the budget is approved
    And the parts are reserved
    And payment is confirmed
    And payment is confirmed again
    Then the work order is InExecution
    And execution was requested only once
