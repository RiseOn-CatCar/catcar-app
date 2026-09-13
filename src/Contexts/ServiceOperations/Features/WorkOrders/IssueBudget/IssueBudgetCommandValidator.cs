namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.IssueBudget;

using FluentValidation;

public sealed class IssueBudgetCommandValidator : AbstractValidator<IssueBudgetCommand>
{
    public IssueBudgetCommandValidator()
    {
        RuleFor(c => c.WorkOrderId).NotEmpty().WithMessage("O identificador da OS é obrigatório.");
    }
}
