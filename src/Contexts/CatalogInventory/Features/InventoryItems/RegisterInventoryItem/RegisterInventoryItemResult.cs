namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.RegisterInventoryItem;

/// <summary>
/// Result of a successful part/supply registration.
/// </summary>
public sealed record RegisterInventoryItemResult(Guid Id, string Sku, string Name, string Description, decimal UnitPrice, int QuantityInStock, int MinimumStockThreshold, bool IsActive);
