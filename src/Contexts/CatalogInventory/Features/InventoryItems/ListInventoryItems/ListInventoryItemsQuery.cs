namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.ListInventoryItems;

/// <summary>
/// Query to list parts/supplies, optionally filtered by active status and/or low-stock condition.
/// </summary>
public sealed record ListInventoryItemsQuery(bool? OnlyActive, bool? OnlyBelowMinimumStock);
