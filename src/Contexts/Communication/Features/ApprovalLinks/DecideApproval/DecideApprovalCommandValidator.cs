namespace CatCar.Contexts.Communication.Features.ApprovalLinks.DecideApproval;

using FluentValidation;

public sealed class DecideApprovalCommandValidator : AbstractValidator<DecideApprovalCommand>
{
    public DecideApprovalCommandValidator()
    {
        RuleFor(c => c.RawToken).NotEmpty().WithMessage("O token de acesso é obrigatório.");

        RuleFor(c => c.Reason)
            .NotEmpty()
            .WithMessage("É necessário informar o motivo da recusa do orçamento.")
            .When(c => !c.Approved);
    }
}
