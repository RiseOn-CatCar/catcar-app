namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.GetInventoryItemById;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="GetInventoryItemByIdQuery"/>.
/// </summary>
public static class GetInventoryItemByIdHandler
{
    public static async Task<Upshot<InventoryItemDetails>> Handle(
        GetInventoryItemByIdQuery query,
        IInventoryItemRepository repository,
        CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(query.InventoryItemId, cancellationToken).ConfigureAwait(false);
        if (item is null)
            return Upshot<InventoryItemDetails>.Fail("Peça/insumo não encontrada.");

        return Upshot<InventoryItemDetails>.Success(
            new InventoryItemDetails(item.Id, item.Sku, item.Name, item.Description, item.UnitPrice.Amount, item.QuantityInStock, item.MinimumStockThreshold, item.IsBelowMinimumStock, item.IsActive));
    }
}
