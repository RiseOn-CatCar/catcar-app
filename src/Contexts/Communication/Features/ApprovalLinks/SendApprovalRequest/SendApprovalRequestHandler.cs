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
            NotificationFailures.Throw(logger, @event.WorkOrderId, @event.BudgetId, @event.CorrelationId, null, Operation, "BudgetSnapshotUnavailable");
        }

        var snapshot = snapshotResult.Value;
        if (!NotificationFailures.IsValidRecipient(snapshot.CustomerEmail))
        {
            NotificationFailures.Throw(logger, @event.WorkOrderId, @event.BudgetId, @event.CorrelationId, null, Operation, "InvalidRecipient");
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

        await NotificationFailures.SendAsync(
            emailSender,
            new EmailMessage(snapshot.CustomerEmail!, snapshot.CustomerName, "CatCar - Orçamento aguardando sua aprovação", body),
            logger,
            @event.WorkOrderId,
            @event.BudgetId,
            @event.CorrelationId,
            null,
            Operation,
            cancellationToken).ConfigureAwait(false);
    }

    private const string Operation = "SendApprovalRequest";

    /// <summary>Marker type used only to scope the <see cref="ILogger{TCategoryName}"/> category.</summary>
    public sealed class Marker;
}
