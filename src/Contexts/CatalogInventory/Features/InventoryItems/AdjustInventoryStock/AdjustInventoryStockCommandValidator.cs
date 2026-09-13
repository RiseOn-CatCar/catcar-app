namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.AdjustInventoryStock;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using FluentValidation;

public sealed class AdjustInventoryStockCommandValidator : AbstractValidator<AdjustInventoryStockCommand>
{
    public AdjustInventoryStockCommandValidator()
    {
        RuleFor(c => c.InventoryItemId)
            .NotEmpty().WithMessage("O identificador da peça/insumo é obrigatório.");

        RuleFor(c => c.MovementType)
            .NotEmpty().WithMessage("O tipo de movimentação é obrigatório.")
            .Must(type => Enum.TryParse<StockMovementType>(type, ignoreCase: true, out _))
            .WithMessage("Tipo de movimentação inválido. Valores aceitos: Entrada, Saida.");

        RuleFor(c => c.Quantity)
            .GreaterThan(0).WithMessage("A quantidade da movimentação deve ser maior que zero.");
    }
}
