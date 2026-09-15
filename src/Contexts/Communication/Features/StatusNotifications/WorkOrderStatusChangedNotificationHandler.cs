namespace CatCar.Contexts.Communication.Features.StatusNotifications;

using CatCar.Contexts.Communication.Integrations;
using CatCar.Contexts.Communication.Notifications;
using CatCar.Contracts.ServiceOperations;
using Microsoft.Extensions.Logging;

/// <summary>
/// Wolverine event handler consuming <see cref="WorkOrderStatusChangedIntegrationEvent"/>.
/// Notifies the customer via email when the work order status transitions to
/// InDiagnosis, AwaitingApproval, InExecution, Completed, or Delivered.
/// </summary>
public static partial class WorkOrderStatusChangedNotificationHandler
{
    private static readonly HashSet<string> NotifiableStatuses =
    [
        "InDiagnosis",
        "AwaitingApproval",
        "InExecution",
        "Completed",
        "Delivered"
    ];

    public static async Task Handle(
        WorkOrderStatusChangedIntegrationEvent @event,
        IServiceOperationsAcl serviceOperationsAcl,
        IEmailSender emailSender,
        ILogger<Marker> logger,
        CancellationToken cancellationToken)
    {
        if (!NotifiableStatuses.Contains(@event.NewStatus)
            || (string.Equals(@event.PreviousStatus, "AwaitingApproval", StringComparison.OrdinalIgnoreCase)
                && string.Equals(@event.NewStatus, "InDiagnosis", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var customerResult = await serviceOperationsAcl.GetCustomerSnapshotAsync(@event.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customerResult.IsFailure || string.IsNullOrWhiteSpace(customerResult.Value.Email))
        {
            LogCustomerEmailNotAvailable(logger, @event.CustomerId, @event.WorkOrderId, @event.NewStatus);
            return;
        }

        var customer = customerResult.Value;
        var (subject, messageBody) = FormatNotificationEmail(@event.NewStatus, @event.WorkOrderId, customer.Name);

        await emailSender.SendAsync(
            new EmailMessage(customer.Email ?? string.Empty, customer.Name, subject, messageBody),
            cancellationToken).ConfigureAwait(false);
    }

    private static (string Subject, string Body) FormatNotificationEmail(string newStatus, Guid workOrderId, string customerName)
    {
        return newStatus switch
        {
            "InDiagnosis" => (
                "CatCar - Diagnóstico Iniciado",
                $"""
                Olá, {customerName}!

                O diagnóstico do seu veículo na Ordem de Serviço (Nº {workOrderId}) foi iniciado.
                Nossos mecânicos estão avaliando os serviços e peças necessários.

                Atenciosamente,
                Equipe CatCar
                """),

            "AwaitingApproval" => (
                "CatCar - Orçamento Aguardando Aprovação",
                $"""
                Olá, {customerName}!

                O orçamento da sua Ordem de Serviço (Nº {workOrderId}) está pronto e aguardando sua aprovação.
                Acesse o link enviado anteriormente para revisar e aprovar o orçamento.

                Atenciosamente,
                Equipe CatCar
                """),

            "InExecution" => (
                "CatCar - Serviço em Execução",
                $"""
                Olá, {customerName}!

                A execução dos serviços da sua Ordem de Serviço (Nº {workOrderId}) foi iniciada.
                Você será notificado assim que o serviço for concluído.

                Atenciosamente,
                Equipe CatCar
                """),

            "Completed" => (
                "CatCar - Serviço Concluído",
                $"""
                Olá, {customerName}!

                Os serviços da sua Ordem de Serviço (Nº {workOrderId}) foram concluídos com sucesso!
                Seu veículo está pronto para retirada.

                Atenciosamente,
                Equipe CatCar
                """),

            "Delivered" => (
                "CatCar - Veículo Entregue",
                $"""
                Olá, {customerName}!

                A sua Ordem de Serviço (Nº {workOrderId}) foi finalizada e o veículo foi entregue.
                Agradecemos pela preferência!

                Atenciosamente,
                Equipe CatCar
                """),

            _ => (
                $"CatCar - Atualização da Ordem de Serviço (Status: {newStatus})",
                $"""
                Olá, {customerName}!

                O status da sua Ordem de Serviço (Nº {workOrderId}) foi alterado para: {newStatus}.

                Atenciosamente,
                Equipe CatCar
                """)
        };
    }

    /// <summary>Marker type used only to scope the <see cref="ILogger{TCategoryName}"/> category.</summary>
    public sealed class Marker
    {
        public static string Category => "WorkOrderStatusChangedNotification";
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Customer email not available for customer {CustomerId} on WorkOrder {WorkOrderId} status transition to {NewStatus}.")]
    private static partial void LogCustomerEmailNotAvailable(ILogger logger, Guid customerId, Guid workOrderId, string newStatus);
}
