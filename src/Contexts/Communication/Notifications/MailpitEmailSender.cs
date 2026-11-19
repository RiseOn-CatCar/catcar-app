namespace CatCar.Contexts.Communication.Notifications;

using System.Net.Http.Json;

/// <summary>Outbound email transport for the local Kubernetes Mailpit instance.</summary>
public sealed class MailpitEmailSender(HttpClient client, string senderAddress) : IEmailSender
{
    public const string HttpClientName = "Mailpit";

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/send", new
        {
            From = new { Email = senderAddress, Name = "CatCar" },
            To = new[] { new { Email = message.ToAddress, Name = message.ToName } },
            message.Subject,
            HTML = message.Body
        }, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
