namespace CatCar.Contexts.Communication.Features.ApprovalLinks.DecideApproval;

using CatCar.Contexts.Communication.Domain.ExternalAccessTokens;
using CatCar.Contexts.Communication.Integrations;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Public, unauthenticated endpoint handler for a customer's approval/rejection decision
/// (AC: endpoint para aprova\u00e7\u00e3o ou recusa - feature 05). Validates the external token first (read-only),
/// then asks ServiceOperations (via the ACL) to record the decision on the Budget/WorkOrder aggregates -
/// the token is only marked Consumed after that call succeeds, so a failed decision leaves the link usable
/// for a retry instead of silently burning it.
/// </summary>
public static class DecideApprovalHandler
{
    public static async Task<Upshot<DecideApprovalResult>> Handle(
        DecideApprovalCommand command,
        IValidator<DecideApprovalCommand> validator,
        IExternalAccessTokenRepository tokenRepository,
        IServiceOperationsAcl serviceOperationsAcl,
        CancellationToken cancellationToken)
    {
        var inputValidation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!inputValidation.IsValid)
            return Upshot<DecideApprovalResult>.Fail(string.Join(" ", inputValidation.Errors.Select(e => e.ErrorMessage)));

        var tokenHash = ExternalAccessToken.ComputeHash(command.RawToken);
        var token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken).ConfigureAwait(false);
        if (token is null)
            return Upshot<DecideApprovalResult>.Fail("Link de aprovação inválido.");

        var validation = token.Validate(command.RawToken);
        if (validation.IsFailure)
        {
            await tokenRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Upshot<DecideApprovalResult>.Fail(validation.Error.Message ?? string.Empty);
        }

        var decisionResult = await serviceOperationsAcl
            .RecordBudgetDecisionAsync(token.BudgetId, command.Approved, command.Reason, cancellationToken)
            .ConfigureAwait(false);

        if (decisionResult.IsFailure)
            return Upshot<DecideApprovalResult>.Fail(decisionResult.Error.Message ?? string.Empty);

        var consumeResult = token.Consume();
        if (consumeResult.IsFailure)
            return Upshot<DecideApprovalResult>.Fail(consumeResult.Error.Message ?? string.Empty);

        await tokenRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var decision = decisionResult.Value;
        return Upshot<DecideApprovalResult>.Success(
            new DecideApprovalResult(decision.BudgetId, decision.BudgetStatus, decision.WorkOrderStatus));
    }
}
