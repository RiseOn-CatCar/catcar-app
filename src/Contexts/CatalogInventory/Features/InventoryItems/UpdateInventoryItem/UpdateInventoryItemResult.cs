namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.UpdateInventoryItem;

/// <summary>
/// Result of a successful part/supply update.
/// </summary>
public sealed record UpdateInventoryItemResult(Guid Id, string Sku, string Name, string Description, decimal UnitPrice, int QuantityInStock, int MinimumStockThreshold, bool IsActive);
