namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.RegisterInventoryItem;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="RegisterInventoryItemCommand"/>.
/// </summary>
public static class RegisterInventoryItemHandler
{
    public static async Task<Upshot<RegisterInventoryItemResult>> Handle(
        RegisterInventoryItemCommand command,
        IValidator<RegisterInventoryItemCommand> validator,
        IInventoryItemRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<RegisterInventoryItemResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var normalizedSku = command.Sku.Trim().ToUpperInvariant();
        if (await repository.ExistsBySkuAsync(normalizedSku, cancellationToken).ConfigureAwait(false))
            return Upshot<RegisterInventoryItemResult>.Fail("Já existe uma peça/insumo cadastrada com este código (SKU).");

        var itemResult = InventoryItem.Register(command.Sku, command.Name, command.Description, command.UnitPrice, command.InitialQuantity, command.MinimumStockThreshold);
        if (itemResult.IsFailure)
            return Upshot<RegisterInventoryItemResult>.Fail(itemResult.Error);

        var item = itemResult.Value;

        await repository.AddAsync(item, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<RegisterInventoryItemResult>.Success(
            new RegisterInventoryItemResult(item.Id, item.Sku, item.Name, item.Description, item.UnitPrice.Amount, item.QuantityInStock, item.MinimumStockThreshold, item.IsActive));
    }
}
