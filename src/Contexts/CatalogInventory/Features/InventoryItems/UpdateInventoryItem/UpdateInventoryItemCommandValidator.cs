namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.UpdateInventoryItem;

using FluentValidation;

public sealed class UpdateInventoryItemCommandValidator : AbstractValidator<UpdateInventoryItemCommand>
{
    public UpdateInventoryItemCommandValidator()
    {
        RuleFor(c => c.InventoryItemId)
            .NotEmpty().WithMessage("O identificador da peça/insumo é obrigatório.");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("O nome da peça/insumo é obrigatório.")
            .MaximumLength(150).WithMessage("O nome deve ter no máximo 150 caracteres.");

        RuleFor(c => c.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(500).WithMessage("A descrição deve ter no máximo 500 caracteres.");

        RuleFor(c => c.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("O preço unitário não pode ser negativo.");

        RuleFor(c => c.MinimumStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("O estoque mínimo não pode ser negativo.");
    }
}
