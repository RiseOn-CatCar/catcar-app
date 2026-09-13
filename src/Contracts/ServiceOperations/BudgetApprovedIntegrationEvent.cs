using CatCar.SharedKernel;

namespace CatCar.Contracts.ServiceOperations;

/// <summary>
/// Integration event raised when a customer approves a budget through the external approval link
/// (feature 05). Published via Wolverine's outbox from <c>RecordApprovalDecisionHandler</c> in the
/// same transaction as the Budget/WorkOrder state transition. Consumed by CatalogInventory (feature 06,
/// to reserve inventory) and by Communication itself (to send the confirmation e-mail).
/// </summary>
public sealed record BudgetApprovedIntegrationEvent(
    Guid BudgetId,
    Guid WorkOrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime ApprovedAt) : IIntegrationEvent;
