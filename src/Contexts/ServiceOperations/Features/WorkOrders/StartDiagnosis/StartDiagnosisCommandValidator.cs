namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.StartDiagnosis;

using FluentValidation;

public sealed class StartDiagnosisCommandValidator : AbstractValidator<StartDiagnosisCommand>
{
    public StartDiagnosisCommandValidator() => RuleFor(c => c.WorkOrderId).NotEmpty().WithMessage("O identificador da OS é obrigatório.");
}
