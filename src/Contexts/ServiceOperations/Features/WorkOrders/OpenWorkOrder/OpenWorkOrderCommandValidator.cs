namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.OpenWorkOrder;

using FluentValidation;

public sealed class OpenWorkOrderCommandValidator : AbstractValidator<OpenWorkOrderCommand>
{
    public OpenWorkOrderCommandValidator()
    {
        RuleFor(c => c.CustomerId).NotEmpty().WithMessage("O cliente é obrigatório para abrir a OS.");

        RuleFor(c => c.VehicleId).NotEmpty().WithMessage("O veículo é obrigatório para abrir a OS.");

        RuleFor(c => c.InitialDescription)
            .NotEmpty().WithMessage("A descrição inicial da OS é obrigatória.")
            .MaximumLength(1000).WithMessage("A descrição inicial deve ter no máximo 1000 caracteres.");
    }
}
