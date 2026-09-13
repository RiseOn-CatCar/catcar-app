namespace CatCar.Contexts.Communication.Features.ApprovalLinks.GetApprovalLinkDetails;

using CatCar.Contexts.Communication.Domain.ExternalAccessTokens;
using CatCar.Contexts.Communication.Integrations;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Resolves the public, unauthenticated approval-link lookup: validates the raw token (constant-time
/// hash comparison, single-use/expiry checks live in the aggregate) and, if still active, fetches the
/// Budget snapshot via the ACL to render its lines/total/expiry to the customer
/// (AC: expira\u00e7\u00e3o/valida\u00e7\u00e3o desse acesso - feature 05).
/// </summary>
public static class GetApprovalLinkDetailsHandler
{
    public static async Task<Upshot<GetApprovalLinkDetailsResult>> Handle(
        GetApprovalLinkDetailsQuery query,
        IExternalAccessTokenRepository tokenRepository,
        IServiceOperationsAcl serviceOperationsAcl,
        CancellationToken cancellationToken)
    {
        var rawToken = query.RawToken;
        var tokenHash = ExternalAccessToken.ComputeHash(rawToken);
        var token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken).ConfigureAwait(false);
        if (token is null)
            return Upshot<GetApprovalLinkDetailsResult>.Fail("Link de aprovação inválido.");

        var validation = token.Validate(rawToken);
        if (validation.IsFailure)
        {
            // Validate() may have lazily flipped the status to Expired - persist that transition.
            await tokenRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Upshot<GetApprovalLinkDetailsResult>.Fail(validation.Error.Message ?? string.Empty);
        }

        var snapshotResult = await serviceOperationsAcl.GetBudgetSnapshotAsync(token.BudgetId, cancellationToken).ConfigureAwait(false);
        if (snapshotResult.IsFailure)
            return Upshot<GetApprovalLinkDetailsResult>.Fail(snapshotResult.Error.Message ?? string.Empty);

        var snapshot = snapshotResult.Value;
        var lines = snapshot.Lines
            .Select(l => new GetApprovalLinkDetailsLine(l.Type, l.Description, l.UnitPrice, l.Quantity, l.LineTotal))
            .ToList();

        return Upshot<GetApprovalLinkDetailsResult>.Success(new GetApprovalLinkDetailsResult(
            snapshot.BudgetId, snapshot.CustomerName, snapshot.BudgetStatus, snapshot.TotalAmount, snapshot.IssuedAt, token.ExpiresAt, lines));
    }
}
