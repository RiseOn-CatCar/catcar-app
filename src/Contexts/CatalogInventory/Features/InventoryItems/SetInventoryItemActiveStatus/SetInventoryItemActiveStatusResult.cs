namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.SetInventoryItemActiveStatus;

/// <summary>
/// Result of a successful part/supply status change.
/// </summary>
public sealed record SetInventoryItemActiveStatusResult(Guid Id, bool IsActive);
