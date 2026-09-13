namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.ListInventoryItems;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;

/// <summary>
/// Wolverine handler for <see cref="ListInventoryItemsQuery"/>.
/// </summary>
public static class ListInventoryItemsHandler
{
    public static async Task<IReadOnlyList<InventoryItemSummary>> Handle(
        ListInventoryItemsQuery query,
        IInventoryItemRepository repository,
        CancellationToken cancellationToken)
    {
        var items = await repository.ListAsync(query.OnlyActive, query.OnlyBelowMinimumStock, cancellationToken).ConfigureAwait(false);

        return items
            .Select(i => new InventoryItemSummary(i.Id, i.Sku, i.Name, i.UnitPrice.Amount, i.QuantityInStock, i.IsBelowMinimumStock, i.IsActive))
            .ToList();
    }
}
