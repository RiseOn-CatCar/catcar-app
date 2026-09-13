namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.AdjustInventoryStock;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="AdjustInventoryStockCommand"/>.
/// AC: controle de estoque (feature 03) - enforces that stock never goes negative.
/// </summary>
public static class AdjustInventoryStockHandler
{
    public static async Task<Upshot<AdjustInventoryStockResult>> Handle(
        AdjustInventoryStockCommand command,
        IValidator<AdjustInventoryStockCommand> validator,
        IInventoryItemRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<AdjustInventoryStockResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var item = await repository.GetByIdAsync(command.InventoryItemId, cancellationToken).ConfigureAwait(false);
        if (item is null)
            return Upshot<AdjustInventoryStockResult>.Fail("Peça/insumo não encontrada.");

        var movementType = Enum.Parse<StockMovementType>(command.MovementType, ignoreCase: true);
        var adjustmentResult = item.AdjustStock(movementType, command.Quantity);
        if (adjustmentResult.IsFailure)
            return Upshot<AdjustInventoryStockResult>.Fail(adjustmentResult.Error);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<AdjustInventoryStockResult>.Success(
            new AdjustInventoryStockResult(item.Id, item.Sku, item.QuantityInStock, item.IsBelowMinimumStock));
    }
}
