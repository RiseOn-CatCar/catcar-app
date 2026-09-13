namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.RecordBudgetDecision;

using CatCar.Contracts.ServiceOperations;
using FluentValidation;

/// <summary>
/// Validates the published-language <see cref="RecordBudgetDecisionCommand"/> before it reaches the
/// domain (AC: consist\u00eancia de entrada - feature 05).
/// </summary>
public sealed class RecordBudgetDecisionCommandValidator : AbstractValidator<RecordBudgetDecisionCommand>
{
    public RecordBudgetDecisionCommandValidator()
    {
        RuleFor(c => c.BudgetId).NotEmpty().WithMessage("O identificador do or\u00e7amento \u00e9 obrigat\u00f3rio.");

        RuleFor(c => c.RejectionReason)
            .NotEmpty()
            .WithMessage("\u00c9 necess\u00e1rio informar o motivo da recusa do or\u00e7amento.")
            .When(c => !c.Approved);
    }
}
