namespace CatCar.Contexts.ServiceOperations.Integrations;

using RiseOn.RailResult.Upshot;

/// <summary>
/// Anti-corruption layer (ACL) port for querying the CatalogInventory context. Per the ratified
/// relationship (Customer-Supplier, OHS + Published Language, ACL inside Atendimento/OS), ServiceOperations
/// never references CatalogInventory's assembly - this port is implemented using Wolverine's in-process
/// message bus against the published-language queries in <c>CatCar.Contracts.CatalogInventory</c>.
/// </summary>
public interface ICatalogInventoryAcl
{
    Task<Upshot<CatalogServiceSnapshot>> GetCatalogedServiceSnapshotAsync(Guid catalogedServiceId, CancellationToken cancellationToken = default);

    Task<Upshot<InventoryItemSnapshot>> GetInventoryItemSnapshotAsync(Guid inventoryItemId, CancellationToken cancellationToken = default);
}
