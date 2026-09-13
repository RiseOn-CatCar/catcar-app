namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedService;

using FluentValidation;

public sealed class AddRequestedServiceCommandValidator : AbstractValidator<AddRequestedServiceCommand>
{
    public AddRequestedServiceCommandValidator()
    {
        RuleFor(c => c.WorkOrderId).NotEmpty().WithMessage("O identificador da OS é obrigatório.");

        RuleFor(c => c.CatalogedServiceId).NotEmpty().WithMessage("O serviço do catálogo é obrigatório.");

        RuleFor(c => c.Quantity).GreaterThan(0).WithMessage("A quantidade do serviço solicitado deve ser maior que zero.");
    }
}
