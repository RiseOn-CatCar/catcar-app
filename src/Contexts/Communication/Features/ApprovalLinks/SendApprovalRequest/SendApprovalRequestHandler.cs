namespace CatCar.Contexts.Communication.Features.ApprovalLinks.SendApprovalRequest;

using CatCar.Contexts.Communication.Domain.ExternalAccessTokens;
using CatCar.Contexts.Communication.Integrations;
using CatCar.Contexts.Communication.Notifications;
using CatCar.Contracts.ServiceOperations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Wolverine event handler subscribing to <see cref="BudgetIssuedIntegrationEvent"/> (published by
/// ServiceOperations/feature 04). Issues a single-use external access token for the budget, persists it,
/// and e-mails the customer a secure approval link (AC: envio do or\u00e7amento ao cliente, link/token externo
/// seguro - feature 05).
/// </summary>
public static partial class SendApprovalRequestHandler
{
    public static async Task Handle(
        BudgetIssuedIntegrationEvent @event,
        IServiceOperationsAcl serviceOperationsAcl,
        IExternalAccessTokenRepository tokenRepository,
        IEmailSender emailSender,
        IOptions<CommunicationOptions> options,
        ILogger<Marker> logger,
        CancellationToken cancellationToken)
    {
        var snapshotResult = await serviceOperationsAcl.GetBudgetSnapshotAsync(@event.BudgetId, cancellationToken).ConfigureAwait(false);
        if (snapshotResult.IsFailure)
        {
            LogSnapshotUnavailable(logger, @event.BudgetId, snapshotResult.Error.Message ?? string.Empty);
            return;
        }

        var snapshot = snapshotResult.Value;
        if (string.IsNullOrWhiteSpace(snapshot.CustomerEmail))
        {
            LogCustomerHasNoEmail(logger, snapshot.CustomerId, @event.BudgetId);
            return;
        }

        var ttl = TimeSpan.FromDays(options.Value.ApprovalLinkTtlDays);
        var (token, rawToken) = ExternalAccessToken.Issue(@event.BudgetId, ttl);

        await tokenRepository.AddAsync(token, cancellationToken).ConfigureAwait(false);
        await tokenRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var approvalUrl = $"{options.Value.ExternalBaseUrl.TrimEnd('/')}/api/v1/communication/approval-links/{rawToken}";

        var body = $"""
            Olá, {snapshot.CustomerName}!

            O orçamento da sua Ordem de Serviço na CatCar está pronto para aprovação.

            Valor total: {snapshot.TotalAmount:C}
            Link válido até: {token.ExpiresAt:dd/MM/yyyy HH:mm} (UTC)

            Acesse o link abaixo para aprovar ou recusar o orçamento:
            {approvalUrl}

            Se você não solicitou este orçamento, ignore esta mensagem.
            """;

        await emailSender.SendAsync(
            new EmailMessage(snapshot.CustomerEmail, snapshot.CustomerName, "CatCar - Orçamento aguardando sua aprovação", body),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Marker type used only to scope the <see cref="ILogger{TCategoryName}"/> category.</summary>
    public sealed class Marker;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível obter o snapshot do orçamento {BudgetId} para enviar o link de aprovação: {Error}")]
    private static partial void LogSnapshotUnavailable(ILogger logger, Guid budgetId, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cliente {CustomerId} não possui e-mail cadastrado; link de aprovação do orçamento {BudgetId} não enviado.")]
    private static partial void LogCustomerHasNoEmail(ILogger logger, Guid customerId, Guid budgetId);
}
