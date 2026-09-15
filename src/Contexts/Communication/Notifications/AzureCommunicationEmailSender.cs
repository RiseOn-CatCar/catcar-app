namespace CatCar.Contexts.Communication.Notifications;

using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of <see cref="IEmailSender"/> using Azure Communication Services Email.
/// Reads connection string from <c>Communication:AzureCommunicationServicesConnectionString</c>
/// or <c>AZURE_COMMUNICATION_CONNECTION_STRING</c>.
/// If connection string is missing or empty, delegates to <see cref="LoggingEmailSender"/>.
/// </summary>
public sealed partial class AzureCommunicationEmailSender : IEmailSender
{
    private readonly EmailClient? _emailClient;
    private readonly LoggingEmailSender _fallbackSender;
    private readonly ILogger<AzureCommunicationEmailSender> _logger;
    private readonly string _senderAddress;

    public AzureCommunicationEmailSender(
        IConfiguration configuration,
        ILogger<AzureCommunicationEmailSender> logger,
        LoggingEmailSender fallbackSender)
    {
        _logger = logger;
        _fallbackSender = fallbackSender;

        var connectionString = configuration["Communication:AzureCommunicationServicesConnectionString"]
            ?? configuration["AZURE_COMMUNICATION_CONNECTION_STRING"]
            ?? Environment.GetEnvironmentVariable("AZURE_COMMUNICATION_CONNECTION_STRING");

        _senderAddress = configuration["Communication:SenderAddress"]
            ?? configuration["COMMUNICATION_SENDER_ADDRESS"]
            ?? "DoNotReply@catcar.com";

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                _emailClient = new EmailClient(connectionString);
            }
            catch (Exception ex)
            {
                LogClientInitFailed(_logger, ex.Message);
                _emailClient = null;
            }
        }
    }

    /// <summary>
    /// Testing constructor allowing an injected <see cref="EmailClient"/>.
    /// </summary>
    public AzureCommunicationEmailSender(
        EmailClient? emailClient,
        LoggingEmailSender fallbackSender,
        ILogger<AzureCommunicationEmailSender> logger,
        string senderAddress = "DoNotReply@catcar.com")
    {
        _emailClient = emailClient;
        _fallbackSender = fallbackSender;
        _logger = logger;
        _senderAddress = senderAddress;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (_emailClient is null)
        {
            await _fallbackSender.SendAsync(message, cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            var emailMessage = new Azure.Communication.Email.EmailMessage(
                senderAddress: _senderAddress,
                recipientAddress: message.ToAddress,
                content: new EmailContent(message.Subject)
                {
                    PlainText = message.Body
                });

            var operation = await _emailClient.SendAsync(
                Azure.WaitUntil.Completed,
                emailMessage,
                cancellationToken).ConfigureAwait(false);

            LogEmailSent(_logger, message.ToAddress, operation.Id);
        }
        catch (Exception ex)
        {
            LogSendFailed(_logger, message.ToAddress, ex.Message);
            await _fallbackSender.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to initialize Azure Communication Services EmailClient: {Error}. Falling back to LoggingEmailSender.")]
    private static partial void LogClientInitFailed(ILogger logger, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent via Azure Communication Services to {ToAddress}. OperationId: {OperationId}")]
    private static partial void LogEmailSent(ILogger logger, string toAddress, string operationId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send email to {ToAddress} via Azure Communication Services: {Error}. Falling back to LoggingEmailSender.")]
    private static partial void LogSendFailed(ILogger logger, string toAddress, string error);
}
