namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.Saga;

using Wolverine;
using Wolverine.Attributes;
using Wolverine.Persistence.Sagas;

public sealed record BudgetLineRequest(string Type, Guid ReferenceId, string Description, decimal UnitPrice, int Quantity);
public sealed record PartQuantity(Guid InventoryItemId, int Quantity);
public sealed record ServiceQuantity(Guid CatalogedServiceId, string Description, int Quantity);

[MessageIdentity("catcar.os.request-budget.v1")]
public sealed record RequestBudget(Guid WorkOrderId, Guid CustomerId, string CustomerName, string CustomerEmail, IReadOnlyList<BudgetLineRequest> Lines, DateTime RequestedAt, Guid CorrelationId);
[MessageIdentity("catcar.os.cancel-budget.v1")]
public sealed record CancelBudget(Guid WorkOrderId, Guid BudgetId, string Reason, Guid CorrelationId);
[MessageIdentity("catcar.os.request-payment.v1")]
public sealed record RequestPayment(Guid WorkOrderId, Guid BudgetId, decimal Amount, string CustomerEmail, Guid CorrelationId);
[MessageIdentity("catcar.os.refund-payment.v1")]
public sealed record RefundPayment(Guid WorkOrderId, Guid BudgetId, string Reason, Guid CorrelationId);
[MessageIdentity("catcar.os.reserve-parts.v1")]
public sealed record ReserveParts(Guid WorkOrderId, IReadOnlyList<PartQuantity> Parts, Guid CorrelationId);
[MessageIdentity("catcar.os.release-parts.v1")]
public sealed record ReleaseParts(Guid WorkOrderId, string Reason, Guid CorrelationId);
[MessageIdentity("catcar.os.start-execution.v1")]
public sealed record StartExecution(Guid WorkOrderId, IReadOnlyList<ServiceQuantity> Services, IReadOnlyList<PartQuantity> Parts, Guid CorrelationId);
[MessageIdentity("catcar.os.work-order-status-changed.v1")]
public sealed record WorkOrderStatusChanged(Guid WorkOrderId, Guid CustomerId, string CustomerName, string? CustomerEmail, string PreviousStatus, string NewStatus, DateTime ChangedAt, Guid CorrelationId, Guid EventId);

[MessageIdentity("catcar.billing.budget-issued.v1")]
public sealed record BudgetIssued([property: SagaIdentity] Guid WorkOrderId, Guid BudgetId, decimal TotalAmount, DateTime IssuedAt, Guid CorrelationId);
[MessageIdentity("catcar.billing.budget-approved.v1")]
public sealed record BudgetApproved([property: SagaIdentity] Guid WorkOrderId, Guid BudgetId, decimal TotalAmount, DateTime ApprovedAt, Guid CorrelationId);
[MessageIdentity("catcar.billing.budget-rejected.v1")]
public sealed record BudgetRejected([property: SagaIdentity] Guid WorkOrderId, Guid BudgetId, string Reason, DateTime RejectedAt, Guid CorrelationId);
[MessageIdentity("catcar.billing.payment-confirmed.v1")]
public sealed record PaymentConfirmed([property: SagaIdentity] Guid WorkOrderId, Guid BudgetId, string PaymentId, decimal Amount, DateTime ConfirmedAt, Guid CorrelationId);
[MessageIdentity("catcar.billing.payment-failed.v1")]
public sealed record PaymentFailed([property: SagaIdentity] Guid WorkOrderId, Guid BudgetId, string Reason, DateTime FailedAt, Guid CorrelationId);
[MessageIdentity("catcar.execution.parts-reserved.v1")]
public sealed record PartsReserved([property: SagaIdentity] Guid WorkOrderId, DateTime ReservedAt, Guid CorrelationId);
[MessageIdentity("catcar.execution.parts-reservation-failed.v1")]
public sealed record PartsReservationFailed([property: SagaIdentity] Guid WorkOrderId, string FailureCode, DateTime FailedAt, Guid CorrelationId);
[MessageIdentity("catcar.execution.execution-completed.v1")]
public sealed record ExecutionCompleted([property: SagaIdentity] Guid WorkOrderId, DateTime CompletedAt, Guid CorrelationId);
[MessageIdentity("catcar.execution.execution-failed.v1")]
public sealed record ExecutionFailed([property: SagaIdentity] Guid WorkOrderId, string Reason, DateTime FailedAt, Guid CorrelationId);

public sealed record ApprovalTimeout : TimeoutMessage
{
    public ApprovalTimeout() : base(TimeSpan.Zero) { }
    public ApprovalTimeout(Guid workOrderId, Guid correlationId, TimeSpan delay) : base(delay) { WorkOrderId = workOrderId; CorrelationId = correlationId; }
    [SagaIdentity] public Guid WorkOrderId { get; set; }
    public Guid CorrelationId { get; set; }
}
public sealed record PaymentTimeout : TimeoutMessage
{
    public PaymentTimeout() : base(TimeSpan.Zero) { }
    public PaymentTimeout(Guid workOrderId, Guid correlationId, TimeSpan delay) : base(delay) { WorkOrderId = workOrderId; CorrelationId = correlationId; }
    [SagaIdentity] public Guid WorkOrderId { get; set; }
    public Guid CorrelationId { get; set; }
}
