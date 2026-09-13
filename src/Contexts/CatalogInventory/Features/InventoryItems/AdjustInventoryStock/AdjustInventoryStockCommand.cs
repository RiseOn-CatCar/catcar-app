namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.AdjustInventoryStock;

/// <summary>
/// Command to apply a stock movement (entrada/saída) to a part/supply.
/// <paramref name="MovementType"/> must be "Entrada" or "Saida" (matches <c>StockMovementType</c>).
/// </summary>
public sealed record AdjustInventoryStockCommand(Guid InventoryItemId, string MovementType, int Quantity, string? Reason);
