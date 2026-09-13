namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.SetInventoryItemActiveStatus;

/// <summary>
/// Command to activate or deactivate a part/supply.
/// </summary>
public sealed record SetInventoryItemActiveStatusCommand(Guid InventoryItemId, bool IsActive);
