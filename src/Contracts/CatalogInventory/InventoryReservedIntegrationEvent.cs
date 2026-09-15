using CatCar.SharedKernel;

namespace CatCar.Contracts.CatalogInventory;

/// <summary>
/// Integration event raised when inventory has been reserved.
/// </summary>
public sealed record InventoryReservedIntegrationEvent(
    Guid WorkOrderId,
    Guid BudgetId,
    IReadOnlyList<ReservedInventoryLine> Items,
    DateTime ReservedAt) : IIntegrationEvent;

public sealed record ReservedInventoryLine(Guid InventoryItemId, int Quantity);
