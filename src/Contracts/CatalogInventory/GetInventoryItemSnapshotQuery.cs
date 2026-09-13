namespace CatCar.Contracts.CatalogInventory;

/// <summary>
/// Published-language query used by other bounded contexts (e.g. ServiceOperations) to obtain a
/// price/description/availability snapshot of an InventoryItem, without depending on the CatalogInventory
/// assembly. Dispatched in-process via Wolverine's message bus.
/// </summary>
public sealed record GetInventoryItemSnapshotQuery(Guid InventoryItemId);
