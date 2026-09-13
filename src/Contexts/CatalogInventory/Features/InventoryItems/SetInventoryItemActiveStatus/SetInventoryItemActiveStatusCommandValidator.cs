namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.SetInventoryItemActiveStatus;

using FluentValidation;

public sealed class SetInventoryItemActiveStatusCommandValidator : AbstractValidator<SetInventoryItemActiveStatusCommand>
{
    public SetInventoryItemActiveStatusCommandValidator()
    {
        RuleFor(c => c.InventoryItemId)
            .NotEmpty().WithMessage("O identificador da peça/insumo é obrigatório.");
    }
}
