using CatCar.SharedKernel;

namespace CatCar.Contracts.CatalogInventory;

/// <summary>
/// Integration event raised when inventory has been consumed.
/// </summary>
public sealed record InventoryConsumedIntegrationEvent(
    Guid WorkOrderId,
    IReadOnlyList<ConsumedInventoryLine> Items,
    DateTime ConsumedAt) : IIntegrationEvent;

public sealed record ConsumedInventoryLine(Guid InventoryItemId, int Quantity);
