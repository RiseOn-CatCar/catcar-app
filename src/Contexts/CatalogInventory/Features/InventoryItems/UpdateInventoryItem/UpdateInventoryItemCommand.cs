namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.UpdateInventoryItem;

/// <summary>
/// Command to update the descriptive, pricing and minimum-stock details of an existing part/supply.
/// Does not change the current stock quantity - see AdjustInventoryStock for that.
/// </summary>
public sealed record UpdateInventoryItemCommand(Guid InventoryItemId, string Name, string Description, decimal UnitPrice, int MinimumStockThreshold);
