namespace CatCar.Contexts.Communication.Features.ApprovalLinks.SendApprovalConfirmation;

using CatCar.Contexts.Communication.Integrations;
using CatCar.Contexts.Communication.Notifications;
using CatCar.Contracts.ServiceOperations;
using Microsoft.Extensions.Logging;

/// <summary>
/// Wolverine event handlers subscribing to <see cref="BudgetApprovedIntegrationEvent"/> and
/// <see cref="BudgetRejectedIntegrationEvent"/>, both published by <c>RecordBudgetDecisionHandler</c>
/// (ServiceOperations) once the customer's decision has been recorded. Sends the customer a confirmation
/// e-mail either way (AC: integração por e-mail - feature 05). Failures are thrown so Wolverine retries durably.
/// </summary>
public static class SendApprovalConfirmationHandler
{
    private const string Operation = "SendApprovalConfirmation";

    public static async Task Handle(
        BudgetApprovedIntegrationEvent @event,
        IServiceOperationsAcl serviceOperationsAcl,
        IEmailSender emailSender,
        ILogger<Marker> logger,
        CancellationToken cancellationToken)
    {
        var snapshot = await GetSnapshotAsync(@event.BudgetId, @event.WorkOrderId, @event.CorrelationId, serviceOperationsAcl, logger, cancellationToken).ConfigureAwait(false);
        var body = $"""
            Olá, {snapshot.CustomerName}!

            Recebemos a aprovação do seu orçamento (Nº {@event.BudgetId}) no valor de {@event.TotalAmount:C}.
            A execução do serviço depende da disponibilidade de estoque das peças necessárias.
            Avisaremos você quando houver atualização do andamento.

            Obrigado por confiar na CatCar!
            """;

        await NotificationFailures.SendAsync(
            emailSender,
            new EmailMessage(snapshot.CustomerEmail!, snapshot.CustomerName, "CatCar - Aprovação do orçamento recebida", body),
            logger,
            @event.WorkOrderId,
            @event.BudgetId,
            @event.CorrelationId,
            null,
            Operation,
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task Handle(
        BudgetRejectedIntegrationEvent @event,
        IServiceOperationsAcl serviceOperationsAcl,
        IEmailSender emailSender,
        ILogger<Marker> logger,
        CancellationToken cancellationToken)
    {
        var snapshot = await GetSnapshotAsync(@event.BudgetId, @event.WorkOrderId, @event.CorrelationId, serviceOperationsAcl, logger, cancellationToken).ConfigureAwait(false);
        var body = $"""
            Olá, {snapshot.CustomerName}!

            Registramos a recusa do orçamento (Nº {@event.BudgetId}).
            Motivo informado: {@event.Reason}

            Se quiser conversar sobre outras opções, entre em contato com a CatCar.
            """;

        await NotificationFailures.SendAsync(
            emailSender,
            new EmailMessage(snapshot.CustomerEmail!, snapshot.CustomerName, "CatCar - Orçamento recusado", body),
            logger,
            @event.WorkOrderId,
            @event.BudgetId,
            @event.CorrelationId,
            null,
            Operation,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<BudgetSnapshot> GetSnapshotAsync(
        Guid budgetId,
        Guid workOrderId,
        Guid correlationId,
        IServiceOperationsAcl serviceOperationsAcl,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var snapshotResult = await serviceOperationsAcl.GetBudgetSnapshotAsync(budgetId, cancellationToken).ConfigureAwait(false);
        if (snapshotResult.IsFailure)
        {
            NotificationFailures.Throw(logger, workOrderId, budgetId, correlationId, null, Operation, "BudgetSnapshotUnavailable");
        }

        var snapshot = snapshotResult.Value;
        if (!NotificationFailures.IsValidRecipient(snapshot.CustomerEmail))
        {
            NotificationFailures.Throw(logger, workOrderId, budgetId, correlationId, null, Operation, "InvalidRecipient");
        }

        return snapshot;
    }

    /// <summary>Marker type used only to scope the <see cref="ILogger{TCategoryName}"/> category.</summary>
    public sealed class Marker;
}
