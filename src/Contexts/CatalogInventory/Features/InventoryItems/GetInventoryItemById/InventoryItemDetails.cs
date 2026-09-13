namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.GetInventoryItemById;

/// <summary>
/// Read model for a single part/supply.
/// </summary>
public sealed record InventoryItemDetails(Guid Id, string Sku, string Name, string Description, decimal UnitPrice, int QuantityInStock, int MinimumStockThreshold, bool IsBelowMinimumStock, bool IsActive);
