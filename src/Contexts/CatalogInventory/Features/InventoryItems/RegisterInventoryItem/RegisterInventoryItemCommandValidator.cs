namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.RegisterInventoryItem;

using FluentValidation;

public sealed class RegisterInventoryItemCommandValidator : AbstractValidator<RegisterInventoryItemCommand>
{
    public RegisterInventoryItemCommandValidator()
    {
        RuleFor(c => c.Sku)
            .NotEmpty().WithMessage("O código (SKU) é obrigatório.")
            .MaximumLength(40).WithMessage("O código (SKU) deve ter no máximo 40 caracteres.");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("O nome da peça/insumo é obrigatório.")
            .MaximumLength(150).WithMessage("O nome deve ter no máximo 150 caracteres.");

        RuleFor(c => c.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(500).WithMessage("A descrição deve ter no máximo 500 caracteres.");

        RuleFor(c => c.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("O preço unitário não pode ser negativo.");

        RuleFor(c => c.InitialQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("A quantidade inicial em estoque não pode ser negativa.");

        RuleFor(c => c.MinimumStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("O estoque mínimo não pode ser negativo.");
    }
}
