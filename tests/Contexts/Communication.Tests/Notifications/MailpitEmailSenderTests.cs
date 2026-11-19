namespace CatCar.Contexts.Communication.Tests.Notifications;

using System.Net;
using System.Text.Json;
using CatCar.Contexts.Communication.Infrastructure;
using CatCar.Contexts.Communication.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

public class MailpitEmailSenderTests
{
    [Fact]
    public async Task SendAsync_WithMessage_PostsMailpitRequest()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://mailpit:8025/") };
        var sender = new MailpitEmailSender(client, "sender@example.com");

        await sender.SendAsync(new EmailMessage("customer@example.com", "Customer", "Subject", "Message body"));

        handler.Method.Should().Be(HttpMethod.Post);
        handler.Uri.Should().Be(new Uri("http://mailpit:8025/api/v1/send"));
        using var json = JsonDocument.Parse(handler.Body!);
        var root = json.RootElement;
        root.GetProperty("from").GetProperty("email").GetString().Should().Be("sender@example.com");
        root.GetProperty("from").GetProperty("name").GetString().Should().Be("CatCar");
        var recipient = root.GetProperty("to")[0];
        recipient.GetProperty("email").GetString().Should().Be("customer@example.com");
        recipient.GetProperty("name").GetString().Should().Be("Customer");
        root.GetProperty("subject").GetString().Should().Be("Subject");
        root.GetProperty("html").GetString().Should().Be("Message body");
    }

    [Fact]
    public async Task SendAsync_WhenMailpitRejectsRequest_PropagatesFailure()
    {
        using var client = new HttpClient(new RecordingHandler(HttpStatusCode.ServiceUnavailable))
        {
            BaseAddress = new Uri("http://mailpit:8025/")
        };
        var sender = new MailpitEmailSender(client, "sender@example.com");

        var act = () => sender.SendAsync(new EmailMessage("customer@example.com", "Customer", "Subject", "Body"));

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task SendAsync_WhenCancelled_PropagatesCancellation()
    {
        using var client = new HttpClient(new RecordingHandler(HttpStatusCode.OK))
        {
            BaseAddress = new Uri("http://mailpit:8025/")
        };
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var sender = new MailpitEmailSender(client, "sender@example.com");

        var act = () => sender.SendAsync(
            new EmailMessage("customer@example.com", "Customer", "Subject", "Body"), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative/path")]
    [InlineData("ftp://mailpit")]
    public void Resolve_InLocalKubernetesWithInvalidMailpitUrl_Fails(string? url)
    {
        var settings = ValidSettings();
        settings["Communication:MailpitBaseUrl"] = url;
        using var provider = BuildProvider("LocalKubernetes", settings);

        var act = () => provider.GetRequiredService<IEmailSender>();

        act.Should().Throw<InvalidOperationException>().WithMessage("*MailpitBaseUrl*");
    }

    [Fact]
    public void Resolve_InLocalKubernetesWithValidSettings_UsesMailpit()
    {
        using var provider = BuildProvider("LocalKubernetes", ValidSettings());

        provider.GetRequiredService<IEmailSender>().Should().BeOfType<MailpitEmailSender>();
    }

    [Theory]
    [InlineData("Communication:SenderAddress")]
    [InlineData("Communication:ExternalBaseUrl")]
    public void Resolve_InLocalKubernetesWithMissingSetting_Fails(string key)
    {
        var settings = ValidSettings();
        settings.Remove(key);
        using var provider = BuildProvider("LocalKubernetes", settings);

        var act = () => provider.GetRequiredService<IEmailSender>();

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{key}*");
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Homologation")]
    public void Resolve_OutsideLocalKubernetesWithMailpitConfigured_Fails(string environmentName)
    {
        using var provider = BuildProvider(environmentName, ValidSettings());

        var act = () => provider.GetRequiredService<IEmailSender>();

        act.Should().Throw<InvalidOperationException>().WithMessage("*MailpitBaseUrl*LocalKubernetes*");
    }

    private static Dictionary<string, string?> ValidSettings() => new()
    {
        ["Communication:MailpitBaseUrl"] = "http://mailpit:8025",
        ["Communication:SenderAddress"] = "sender@example.com",
        ["Communication:ExternalBaseUrl"] = "http://localhost:8080"
    };

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

    private sealed class RecordingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? Uri { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode);
        }
    }
}
