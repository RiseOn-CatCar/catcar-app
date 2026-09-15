namespace CatCar.Contexts.Communication.Notifications;

using Microsoft.Extensions.Logging;
using RiseOn.AutoInject;

/// <summary>
/// Simulated <see cref="IEmailSender"/> implementation for the CatCar Tech Challenge: no real SMTP/SES
/// provider is wired up (fictitious workshop, no real customers to reach), so outbound e-mails are logged
/// at Information level instead of actually dispatched. Swapping in a real provider only requires a new
/// <see cref="IEmailSender"/> implementation - no Feature code needs to change (AC: integração por e-mail).
/// </summary>
public sealed partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        LogSimulatedEmail(logger, message.ToName, message.ToAddress, message.Subject, message.Body);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[E-mail simulado] Para: {ToName} <{ToAddress}> | Assunto: {Subject}\n{Body}")]
    private static partial void LogSimulatedEmail(ILogger logger, string toName, string toAddress, string subject, string body);
}
