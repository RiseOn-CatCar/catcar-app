using CatCar.SharedKernel;

namespace CatCar.Contracts.ServiceOperations;

/// <summary>
/// Integration event raised when a customer rejects a budget through the external approval link
/// (feature 05). Published via Wolverine's outbox from <c>RecordApprovalDecisionHandler</c> in the
/// same transaction as the ExternalAccessToken consumption. Consumed by ServiceOperations (feature 06)
/// to transition the Budget/WorkOrder to their rejected states.
/// </summary>
public sealed record BudgetRejectedIntegrationEvent(
    Guid BudgetId,
    Guid WorkOrderId,
    Guid CustomerId,
    string Reason,
    DateTime RejectedAt) : IIntegrationEvent;
