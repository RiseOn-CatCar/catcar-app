namespace CatCar.Contexts.Communication.Notifications;

/// <summary>
/// Port for the outbound e-mail channel used to notify customers (AC: integração por e-mail - feature 05).
/// Abstracts the concrete transport so a real SMTP/SES/SendGrid provider can be plugged in later without
/// touching the Features that depend on this port.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// A plain-text/HTML e-mail message addressed to a single recipient.
/// </summary>
public sealed record EmailMessage(string ToAddress, string ToName, string Subject, string Body);
