namespace CatCar.Contexts.Communication.Notifications;

using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;

/// <summary>Outbound email transport backed by Azure Communication Services.</summary>
public sealed class AzureCommunicationEmailSender : IEmailSender
{
    private readonly EmailClient _emailClient;
    private readonly string _senderAddress;

    public AzureCommunicationEmailSender(IConfiguration configuration)
        : this(
            new EmailClient(configuration["Communication:AzureCommunicationServicesConnectionString"]
                ?? throw new InvalidOperationException("Communication:AzureCommunicationServicesConnectionString is required.")),
            configuration["Communication:SenderAddress"]
                ?? throw new InvalidOperationException("Communication:SenderAddress is required."))
    {
    }

    public AzureCommunicationEmailSender(EmailClient emailClient, string senderAddress)
    {
        _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
        _senderAddress = senderAddress;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var emailMessage = new Azure.Communication.Email.EmailMessage(
            senderAddress: _senderAddress,
            recipientAddress: message.ToAddress,
            content: new EmailContent(message.Subject) { PlainText = message.Body });

        await _emailClient.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken).ConfigureAwait(false);
    }
}
