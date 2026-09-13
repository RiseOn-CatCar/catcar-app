namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.SetInventoryItemActiveStatus;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.SharedKernel;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="SetInventoryItemActiveStatusCommand"/>.
/// </summary>
public static class SetInventoryItemActiveStatusHandler
{
    public static async Task<Upshot<SetInventoryItemActiveStatusResult>> Handle(
        SetInventoryItemActiveStatusCommand command,
        IValidator<SetInventoryItemActiveStatusCommand> validator,
        IInventoryItemRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<SetInventoryItemActiveStatusResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var item = await repository.GetByIdAsync(command.InventoryItemId, cancellationToken).ConfigureAwait(false);
        if (item is null)
            return Upshot<SetInventoryItemActiveStatusResult>.Fail("Peça/insumo não encontrada.");

        try
        {
            if (command.IsActive)
                item.Activate();
            else
                item.Deactivate();
        }
        catch (BusinessRuleViolatedException ex)
        {
            return Upshot<SetInventoryItemActiveStatusResult>.Fail(ex.Message);
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<SetInventoryItemActiveStatusResult>.Success(new SetInventoryItemActiveStatusResult(item.Id, item.IsActive));
    }
}
