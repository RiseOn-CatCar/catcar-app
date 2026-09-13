namespace CatCar.Contracts.CatalogInventory;

/// <summary>
/// Published-language response to <see cref="GetCatalogedServiceSnapshotQuery"/>.
/// </summary>
public sealed record CatalogedServiceSnapshotResponse(bool Found, Guid Id, string Description, decimal UnitPrice, bool IsActive);
