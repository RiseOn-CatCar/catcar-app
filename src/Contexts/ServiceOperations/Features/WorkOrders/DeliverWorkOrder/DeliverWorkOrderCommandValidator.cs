namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.DeliverWorkOrder;

using FluentValidation;

public sealed class DeliverWorkOrderCommandValidator : AbstractValidator<DeliverWorkOrderCommand>
{
    public DeliverWorkOrderCommandValidator() => RuleFor(c => c.WorkOrderId).NotEmpty().WithMessage("O identificador da OS é obrigatório.");
}
