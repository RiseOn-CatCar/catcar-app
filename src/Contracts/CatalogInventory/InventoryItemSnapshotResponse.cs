namespace CatCar.Contracts.CatalogInventory;

/// <summary>
/// Published-language response to <see cref="GetInventoryItemSnapshotQuery"/>.
/// </summary>
public sealed record InventoryItemSnapshotResponse(bool Found, Guid Id, string Description, decimal UnitPrice, bool IsActive, int QuantityInStock);
