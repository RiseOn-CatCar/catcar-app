namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.RegisterInventoryItem;

/// <summary>
/// Command to register a new part/supply with an initial stock quantity.
/// </summary>
public sealed record RegisterInventoryItemCommand(string Sku, string Name, string Description, decimal UnitPrice, int InitialQuantity, int MinimumStockThreshold);
