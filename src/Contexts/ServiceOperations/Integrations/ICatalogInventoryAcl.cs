namespace CatCar.Contexts.ServiceOperations.Integrations;

using RiseOn.RailResult.Upshot;

/// <summary>
/// Anti-corruption layer for reading catalog information from the execution service.
/// </summary>
public interface ICatalogInventoryAcl
{
    Task<Upshot<CatalogServiceSnapshot>> GetCatalogedServiceSnapshotAsync(Guid catalogedServiceId, CancellationToken cancellationToken = default);

    Task<Upshot<InventoryItemSnapshot>> GetInventoryItemSnapshotAsync(Guid inventoryItemId, CancellationToken cancellationToken = default);
}
