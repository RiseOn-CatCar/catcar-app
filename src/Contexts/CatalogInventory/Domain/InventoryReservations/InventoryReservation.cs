namespace CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;

using CatCar.SharedKernel;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Status of an inventory reservation.
/// </summary>
public enum InventoryReservationStatus
{
    Reserved,
    Consumed,
    Released
}

/// <summary>
/// Aggregate root representing reserved inventory for a specific WorkOrder / Budget.
/// </summary>
public sealed class InventoryReservation : Entity<Guid>, IAggregateRoot
{
    public Guid WorkOrderId { get; private set; }

    public Guid BudgetId { get; private set; }

    public Guid InventoryItemId { get; private set; }

    public int Quantity { get; private set; }

    public InventoryReservationStatus Status { get; private set; }

    public DateTime ReservedAt { get; private set; }

    public DateTime? ConsumedAt { get; private set; }

    public DateTime? ReleasedAt { get; private set; }

    private InventoryReservation(
        Guid id,
        Guid workOrderId,
        Guid budgetId,
        Guid inventoryItemId,
        int quantity,
        DateTime reservedAt)
        : base(id)
    {
        WorkOrderId = workOrderId;
        BudgetId = budgetId;
        InventoryItemId = inventoryItemId;
        Quantity = quantity;
        Status = InventoryReservationStatus.Reserved;
        ReservedAt = reservedAt;
    }

#pragma warning disable CS8618
    private InventoryReservation()
        : base(Guid.Empty)
    {
    }
#pragma warning restore CS8618

    public static Upshot<InventoryReservation> Create(
        Guid workOrderId,
        Guid budgetId,
        Guid inventoryItemId,
        int quantity,
        DateTime reservedAt)
    {
        if (workOrderId == Guid.Empty)
            return Upshot<InventoryReservation>.Fail("WorkOrderId é obrigatório.");

        if (budgetId == Guid.Empty)
            return Upshot<InventoryReservation>.Fail("BudgetId é obrigatório.");

        if (inventoryItemId == Guid.Empty)
            return Upshot<InventoryReservation>.Fail("InventoryItemId é obrigatório.");

        if (quantity <= 0)
            return Upshot<InventoryReservation>.Fail("Quantidade reservada deve ser maior que zero.");

        return Upshot<InventoryReservation>.Success(
            new InventoryReservation(Guid.CreateVersion7(), workOrderId, budgetId, inventoryItemId, quantity, reservedAt));
    }

    public Upshot MarkAsConsumed(DateTime consumedAt)
    {
        if (Status != InventoryReservationStatus.Reserved)
            return Upshot.Fail($"Não é possível consumir uma reserva com status {Status}.");

        Status = InventoryReservationStatus.Consumed;
        ConsumedAt = consumedAt;
        return Upshot.Success();
    }

    public Upshot Release(DateTime releasedAt)
    {
        if (Status != InventoryReservationStatus.Reserved)
            return Upshot.Fail($"Não é possível liberar uma reserva com status {Status}.");

        Status = InventoryReservationStatus.Released;
        ReleasedAt = releasedAt;
        return Upshot.Success();
    }
}
