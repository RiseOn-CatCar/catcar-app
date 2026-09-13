using CatCar.SharedKernel;

namespace CatCar.Contracts.ServiceOperations;

/// <summary>
/// Integration event raised when a budget has been issued and is awaiting the customer's approval.
/// Published via Wolverine's outbox when <c>Budget.Issue</c> completes (feature 04). Consumed by the
/// Communication context (feature 05) to send the approval link to the customer.
/// </summary>
public sealed record BudgetIssuedIntegrationEvent(
    Guid BudgetId,
    Guid WorkOrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime IssuedAt) : IIntegrationEvent;
