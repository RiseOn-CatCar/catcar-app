namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.GetInventoryItemById;

/// <summary>
/// Query to fetch a single part/supply by its identifier.
/// </summary>
public sealed record GetInventoryItemByIdQuery(Guid InventoryItemId);
