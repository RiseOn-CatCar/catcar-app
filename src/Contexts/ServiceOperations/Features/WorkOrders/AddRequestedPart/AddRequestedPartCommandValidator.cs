namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedPart;

using FluentValidation;

public sealed class AddRequestedPartCommandValidator : AbstractValidator<AddRequestedPartCommand>
{
    public AddRequestedPartCommandValidator()
    {
        RuleFor(c => c.WorkOrderId).NotEmpty().WithMessage("O identificador da OS é obrigatório.");

        RuleFor(c => c.InventoryItemId).NotEmpty().WithMessage("A peça/insumo é obrigatória.");

        RuleFor(c => c.Quantity).GreaterThan(0).WithMessage("A quantidade da peça/insumo solicitada deve ser maior que zero.");
    }
}
