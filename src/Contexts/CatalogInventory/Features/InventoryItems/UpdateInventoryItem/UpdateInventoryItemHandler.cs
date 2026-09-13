namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.UpdateInventoryItem;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="UpdateInventoryItemCommand"/>.
/// </summary>
public static class UpdateInventoryItemHandler
{
    public static async Task<Upshot<UpdateInventoryItemResult>> Handle(
        UpdateInventoryItemCommand command,
        IValidator<UpdateInventoryItemCommand> validator,
        IInventoryItemRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<UpdateInventoryItemResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var item = await repository.GetByIdAsync(command.InventoryItemId, cancellationToken).ConfigureAwait(false);
        if (item is null)
            return Upshot<UpdateInventoryItemResult>.Fail("Peça/insumo não encontrada.");

        var updateResult = item.UpdateDetails(command.Name, command.Description, command.UnitPrice, command.MinimumStockThreshold);
        if (updateResult.IsFailure)
            return Upshot<UpdateInventoryItemResult>.Fail(updateResult.Error);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<UpdateInventoryItemResult>.Success(
            new UpdateInventoryItemResult(item.Id, item.Sku, item.Name, item.Description, item.UnitPrice.Amount, item.QuantityInStock, item.MinimumStockThreshold, item.IsActive));
    }
}
