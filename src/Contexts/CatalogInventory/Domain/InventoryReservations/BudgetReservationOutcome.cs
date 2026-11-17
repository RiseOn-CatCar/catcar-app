namespace CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;

public enum ReservationOutcome { Reserved, NoParts, Failed }

public enum InventoryReservationFailureCode
{
    InsufficientStock, InventoryItemNotFound, InventoryItemInactive,
    BudgetSnapshotNotFound, InvalidBudgetLines, ReservationHistoryMismatch
}

public sealed class BudgetReservationOutcome
{
    public Guid BudgetId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public ReservationOutcome Outcome { get; private set; }
    public InventoryReservationFailureCode? FailureCode { get; private set; }
    public DateTime ResolvedAt { get; private set; }
    public Guid CorrelationId { get; private set; }

    private BudgetReservationOutcome() { }

    public BudgetReservationOutcome(Guid budgetId, Guid workOrderId, ReservationOutcome outcome,
        InventoryReservationFailureCode? failureCode, DateTime resolvedAt, Guid correlationId)
    {
        if ((outcome == ReservationOutcome.Failed) != failureCode.HasValue)
            throw new ArgumentException("A failure code is required only for failed outcomes.", nameof(failureCode));
        BudgetId = budgetId;
        WorkOrderId = workOrderId;
        Outcome = outcome;
        FailureCode = failureCode;
        ResolvedAt = resolvedAt;
        CorrelationId = correlationId;
    }
}
