namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.CompleteWorkOrder;

using FluentValidation;

public sealed class CompleteWorkOrderCommandValidator : AbstractValidator<CompleteWorkOrderCommand>
{
    public CompleteWorkOrderCommandValidator() => RuleFor(c => c.WorkOrderId).NotEmpty().WithMessage("O identificador da OS é obrigatório.");
}
