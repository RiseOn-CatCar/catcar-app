namespace CatCar.Contexts.Communication.Notifications;

using Microsoft.Extensions.Logging;

/// <summary>Development-only simulated email transport; message content and recipient are never logged.</summary>
public sealed partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogSimulatedEmail(logger);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Simulated email delivery.")]
    private static partial void LogSimulatedEmail(ILogger logger);
}
