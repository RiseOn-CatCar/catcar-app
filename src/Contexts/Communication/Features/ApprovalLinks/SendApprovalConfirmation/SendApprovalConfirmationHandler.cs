namespace CatCar.Contexts.Communication.Features.ApprovalLinks.SendApprovalConfirmation;

using CatCar.Contexts.Communication.Integrations;
using CatCar.Contexts.Communication.Notifications;
using CatCar.Contracts.ServiceOperations;
using Microsoft.Extensions.Logging;

/// <summary>
/// Wolverine event handlers subscribing to <see cref="BudgetApprovedIntegrationEvent"/> and
/// <see cref="BudgetRejectedIntegrationEvent"/>, both published by <c>RecordBudgetDecisionHandler</c>
/// (ServiceOperations) once the customer's decision has been recorded. Sends the customer a confirmation
/// e-mail either way (AC: integração por e-mail - feature 05).
/// </summary>
public static partial class SendApprovalConfirmationHandler
{
    public static async Task Handle(
        BudgetApprovedIntegrationEvent @event,
        IServiceOperationsAcl serviceOperationsAcl,
        IEmailSender emailSender,
        ILogger<Marker> logger,
        CancellationToken cancellationToken)
    {
        var snapshotResult = await serviceOperationsAcl.GetBudgetSnapshotAsync(@event.BudgetId, cancellationToken).ConfigureAwait(false);
        if (snapshotResult.IsFailure || string.IsNullOrWhiteSpace(snapshotResult.Value.CustomerEmail))
        {
            LogConfirmationEmailNotSent(logger, "aprovação", @event.BudgetId);
            return;
        }

        var snapshot = snapshotResult.Value;
        var body = $"""
            Olá, {snapshot.CustomerName}!

            Seu orçamento (Nº {@event.BudgetId}) no valor de {@event.TotalAmount:C} foi aprovado com sucesso.
            A CatCar já está providenciando a execução do serviço.

            Obrigado por confiar na CatCar!
            """;

        await emailSender.SendAsync(
            new EmailMessage(snapshot.CustomerEmail!, snapshot.CustomerName, "CatCar - Orçamento aprovado", body),
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task Handle(
        BudgetRejectedIntegrationEvent @event,
        IServiceOperationsAcl serviceOperationsAcl,
        IEmailSender emailSender,
        ILogger<Marker> logger,
        CancellationToken cancellationToken)
    {
        var snapshotResult = await serviceOperationsAcl.GetBudgetSnapshotAsync(@event.BudgetId, cancellationToken).ConfigureAwait(false);
        if (snapshotResult.IsFailure || string.IsNullOrWhiteSpace(snapshotResult.Value.CustomerEmail))
        {
            LogConfirmationEmailNotSent(logger, "recusa", @event.BudgetId);
            return;
        }

        var snapshot = snapshotResult.Value;
        var body = $"""
            Olá, {snapshot.CustomerName}!

            Registramos a recusa do orçamento (Nº {@event.BudgetId}).
            Motivo informado: {@event.Reason}

            Se quiser conversar sobre outras opções, entre em contato com a CatCar.
            """;

        await emailSender.SendAsync(
            new EmailMessage(snapshot.CustomerEmail!, snapshot.CustomerName, "CatCar - Orçamento recusado", body),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Marker type used only to scope the <see cref="ILogger{TCategoryName}"/> category.</summary>
    public sealed class Marker;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível enviar a confirmação de {Decision} do orçamento {BudgetId}.")]
    private static partial void LogConfirmationEmailNotSent(ILogger logger, string decision, Guid budgetId);
}
