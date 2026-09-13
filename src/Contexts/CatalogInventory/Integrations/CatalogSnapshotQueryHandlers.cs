namespace CatCar.Contexts.CatalogInventory.Integrations;

using CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;
using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contracts.CatalogInventory;

/// <summary>
/// Wolverine handlers answering the published-language snapshot queries defined in
/// <c>CatCar.Contracts.CatalogInventory</c>. This is the Open Host Service (OHS) side of the
/// Customer-Supplier relationship with Atendimento/OS: consumers (ServiceOperations) invoke these
/// queries via <c>IMessageBus</c> without ever referencing CatalogInventory's assembly directly.
/// </summary>
public static class CatalogSnapshotQueryHandlers
{
    public static async Task<CatalogedServiceSnapshotResponse> Handle(
        GetCatalogedServiceSnapshotQuery query,
        ICatalogedServiceRepository repository,
        CancellationToken cancellationToken)
    {
        var service = await repository.GetByIdAsync(query.CatalogedServiceId, cancellationToken).ConfigureAwait(false);

        return service is null
            ? new CatalogedServiceSnapshotResponse(false, query.CatalogedServiceId, string.Empty, 0m, false)
            : new CatalogedServiceSnapshotResponse(true, service.Id, service.Name, service.Price.Amount, service.IsActive);
    }

    public static async Task<InventoryItemSnapshotResponse> Handle(
        GetInventoryItemSnapshotQuery query,
        IInventoryItemRepository repository,
        CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(query.InventoryItemId, cancellationToken).ConfigureAwait(false);

        return item is null
            ? new InventoryItemSnapshotResponse(false, query.InventoryItemId, string.Empty, 0m, false, 0)
            : new InventoryItemSnapshotResponse(true, item.Id, item.Name, item.UnitPrice.Amount, item.IsActive, item.QuantityInStock);
    }
}
