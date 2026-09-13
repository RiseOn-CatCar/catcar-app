namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.AdjustInventoryStock;

/// <summary>
/// Result of a successful stock movement.
/// </summary>
public sealed record AdjustInventoryStockResult(Guid Id, string Sku, int QuantityInStock, bool IsBelowMinimumStock);
