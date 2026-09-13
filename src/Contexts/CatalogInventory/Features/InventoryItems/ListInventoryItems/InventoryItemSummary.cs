namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.ListInventoryItems;

/// <summary>
/// Read model for a part/supply list item.
/// </summary>
public sealed record InventoryItemSummary(Guid Id, string Sku, string Name, decimal UnitPrice, int QuantityInStock, bool IsBelowMinimumStock, bool IsActive);
