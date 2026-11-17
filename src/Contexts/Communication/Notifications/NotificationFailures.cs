namespace CatCar.Contexts.Communication.Notifications;

using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

internal static partial class NotificationFailures
{
    public static bool IsValidRecipient(string? address) =>
        !string.IsNullOrWhiteSpace(address)
        && MailAddress.TryCreate(address, out var parsed)
        && string.Equals(parsed.Address, address, StringComparison.OrdinalIgnoreCase);

    [DoesNotReturn]
    public static void Throw(ILogger logger, Guid workOrderId, Guid? budgetId, Guid correlationId, Guid? eventId, string operation, string failureCode)
    {
        LogFailure(logger, workOrderId, budgetId, correlationId, eventId, operation, failureCode);
        throw new InvalidOperationException($"Communication notification failed: {failureCode}.");
    }

    public static async Task SendAsync(IEmailSender sender, EmailMessage message, ILogger logger, Guid workOrderId, Guid? budgetId, Guid correlationId, Guid? eventId, string operation, CancellationToken cancellationToken)
    {
        try
        {
            await sender.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogFailure(logger, workOrderId, budgetId, correlationId, eventId, operation, "EmailProviderFailure");
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "WorkOrderIntegrationFailed WorkOrderId={WorkOrderId} BudgetId={BudgetId} CorrelationId={CorrelationId} EventId={EventId} Component=Communication Operation={Operation} FailureCode={FailureCode}")]
    private static partial void LogFailure(ILogger logger, Guid workOrderId, Guid? budgetId, Guid correlationId, Guid? eventId, string operation, string failureCode);
}
