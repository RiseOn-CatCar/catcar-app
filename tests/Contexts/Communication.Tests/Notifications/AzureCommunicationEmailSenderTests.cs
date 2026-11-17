namespace CatCar.Contexts.Communication.Tests.Notifications;

using EmailMessage = CatCar.Contexts.Communication.Notifications.EmailMessage;
using Azure;
using Azure.Communication.Email;
using CatCar.Contexts.Communication.Infrastructure;
using CatCar.Contexts.Communication.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

public class AzureCommunicationEmailSenderTests
{
    private const string Secret = "endpoint=https://acs.example.com/;accesskey=c2VjcmV0";

    [Fact]
    public async Task SendAsync_WhenProviderFails_ShouldPropagateWithoutFallback()
    {
        var client = Substitute.For<EmailClient>();
        client.SendAsync(Arg.Any<WaitUntil>(), Arg.Any<Azure.Communication.Email.EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<EmailSendOperation>(new RequestFailedException("provider down")));
        var sender = new AzureCommunicationEmailSender(client, "DoNotReply@catcar.com");

        var act = () => sender.SendAsync(new EmailMessage("cliente@example.com", "Cliente", "Assunto", "Corpo"), CancellationToken.None);

        await act.Should().ThrowAsync<RequestFailedException>();
    }

    [Fact]
    public void Constructor_WhenSenderAddressMissing_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Communication:AzureCommunicationServicesConnectionString"] = Secret })
            .Build();

        var act = () => new AzureCommunicationEmailSender(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Homologation")]
    public void Resolve_InNonDevelopmentWithoutConnectionString_ShouldFailFast(string environmentName)
    {
        using var provider = BuildProvider(environmentName, []);

        var act = () => provider.GetRequiredService<IEmailSender>();

        act.Should().Throw<InvalidOperationException>().WithMessage("*AzureCommunicationServicesConnectionString*");
    }

    [Theory]
    [InlineData("Production", "Communication:SenderAddress")]
    [InlineData("Homologation", "Communication:ExternalBaseUrl")]
    public void Resolve_InNonDevelopmentWithMissingRequiredSetting_ShouldFailWithoutSecret(string environmentName, string missingKey)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Communication:AzureCommunicationServicesConnectionString"] = Secret,
            ["Communication:SenderAddress"] = "DoNotReply@catcar.com",
            ["Communication:ExternalBaseUrl"] = "https://catcar.example.com"
        };
        settings.Remove(missingKey);
        using var provider = BuildProvider(environmentName, settings);

        var act = () => provider.GetRequiredService<IEmailSender>();

        act.Should().Throw<InvalidOperationException>().Where(e => !e.Message.Contains("accesskey", StringComparison.Ordinal));
    }

    [Fact]
    public void Resolve_InNonDevelopmentWithInvalidSenderAddress_ShouldFail()
    {
        using var provider = BuildProvider("Production", new Dictionary<string, string?>
        {
            ["Communication:AzureCommunicationServicesConnectionString"] = Secret,
            ["Communication:SenderAddress"] = "not-an-email",
            ["Communication:ExternalBaseUrl"] = "https://catcar.example.com"
        });

        var act = () => provider.GetRequiredService<IEmailSender>();

        act.Should().Throw<InvalidOperationException>().WithMessage("*SenderAddress*");
    }

    [Fact]
    public void Resolve_InDevelopmentWithoutConnectionString_ShouldUseSimulatedSender()
    {
        using var provider = BuildProvider("Development", []);

        provider.GetRequiredService<IEmailSender>().Should().BeOfType<LoggingEmailSender>();
    }

    [Fact]
    public void Resolve_InDevelopmentWithConnectionString_ShouldUseAzureSender()
    {
        using var provider = BuildProvider("Development", new Dictionary<string, string?>
        {
            ["Communication:AzureCommunicationServicesConnectionString"] = Secret,
            ["Communication:SenderAddress"] = "DoNotReply@catcar.com"
        });

        provider.GetRequiredService<IEmailSender>().Should().BeOfType<AzureCommunicationEmailSender>();
    }

    [Fact]
    public async Task SendAsync_WithSimulatedSender_ShouldNotLogRecipientNameSubjectOrBody()
    {
        var logger = new CapturingLogger<LoggingEmailSender>();
        var sender = new LoggingEmailSender(logger);

        await sender.SendAsync(new EmailMessage("cliente@example.com", "Cliente Secreto", "Assunto Secreto", "https://x/approval-links/token"), CancellationToken.None);

        logger.Entries.Should().ContainSingle()
            .Which.Message.Should().NotContainAny("cliente@example.com", "Cliente Secreto", "Assunto Secreto", "token");
    }

    private static ServiceProvider BuildProvider(string environmentName, Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(environment);
        services.AddCommunicationInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
