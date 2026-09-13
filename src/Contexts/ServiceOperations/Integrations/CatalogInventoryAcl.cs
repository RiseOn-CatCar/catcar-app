namespace CatCar.Contexts.ServiceOperations.Integrations;

using CatCar.Contracts.CatalogInventory;
using RiseOn.AutoInject;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Wolverine-based implementation of <see cref="ICatalogInventoryAcl"/>. Dispatches published-language
/// queries in-process (no HTTP, no shared entities) and translates the responses into ServiceOperations'
/// own local read models.
/// </summary>
[InjectService(ServiceLifetimeType.Scoped, CollectionName = "ServiceOperations")]
public sealed class CatalogInventoryAcl(IMessageBus bus) : ICatalogInventoryAcl
{
    public async Task<Upshot<CatalogServiceSnapshot>> GetCatalogedServiceSnapshotAsync(Guid catalogedServiceId, CancellationToken cancellationToken = default)
    {
        var response = await bus.InvokeAsync<CatalogedServiceSnapshotResponse>(
            new GetCatalogedServiceSnapshotQuery(catalogedServiceId), cancellationToken).ConfigureAwait(false);

        if (!response.Found)
            return Upshot<CatalogServiceSnapshot>.Fail("Serviço não encontrado no catálogo.");

        if (!response.IsActive)
            return Upshot<CatalogServiceSnapshot>.Fail("O serviço solicitado está inativo no catálogo.");

        return Upshot<CatalogServiceSnapshot>.Success(
            new CatalogServiceSnapshot(response.Id, response.Description, response.UnitPrice, response.IsActive));
    }

    public async Task<Upshot<InventoryItemSnapshot>> GetInventoryItemSnapshotAsync(Guid inventoryItemId, CancellationToken cancellationToken = default)
    {
        var response = await bus.InvokeAsync<InventoryItemSnapshotResponse>(
            new GetInventoryItemSnapshotQuery(inventoryItemId), cancellationToken).ConfigureAwait(false);

        if (!response.Found)
            return Upshot<InventoryItemSnapshot>.Fail("Peça/insumo não encontrado no estoque.");

        if (!response.IsActive)
            return Upshot<InventoryItemSnapshot>.Fail("A peça/insumo solicitada está inativa no estoque.");

        return Upshot<InventoryItemSnapshot>.Success(
            new InventoryItemSnapshot(response.Id, response.Description, response.UnitPrice, response.IsActive, response.QuantityInStock));
    }
}
