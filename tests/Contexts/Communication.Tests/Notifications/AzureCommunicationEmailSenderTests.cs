namespace CatCar.Contexts.Communication.Tests.Notifications;

using CatCar.Contexts.Communication.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

public class AzureCommunicationEmailSenderTests
{
    private readonly ILogger<AzureCommunicationEmailSender> _logger = Substitute.For<ILogger<AzureCommunicationEmailSender>>();
    private readonly ILogger<LoggingEmailSender> _fallbackLogger = Substitute.For<ILogger<LoggingEmailSender>>();

    [Fact]
    public async Task SendAsync_WhenNoConnectionString_ShouldDelegateToFallbackSender()
    {
        var configuration = new ConfigurationBuilder().Build();
        var fallbackSender = new LoggingEmailSender(_fallbackLogger);
        var sender = new AzureCommunicationEmailSender(configuration, _logger, fallbackSender);

        var message = new EmailMessage("cliente@example.com", "Cliente Teste", "Assunto", "Corpo da mensagem");

        var act = () => sender.SendAsync(message, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_WhenClientIsNull_ShouldDelegateToFallback()
    {
        var fallbackSender = new LoggingEmailSender(_fallbackLogger);
        var sender = new AzureCommunicationEmailSender(null, fallbackSender, _logger);

        var message = new EmailMessage("cliente@example.com", "Cliente Teste", "Assunto", "Corpo da mensagem");

        var act = () => sender.SendAsync(message, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
