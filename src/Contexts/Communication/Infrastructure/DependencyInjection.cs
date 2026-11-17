using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CatCar.Contexts.Communication.Infrastructure.Persistence;
using CatCar.Contexts.Communication.Notifications;

namespace CatCar.Contexts.Communication.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCommunicationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CommunicationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("catcar"));
            options.AddInterceptors(new AuditInterceptor());
        });

        services.AddHttpClient(MailpitEmailSender.HttpClientName);

        services.AddSingleton<IEmailSender>(provider =>
        {
            var environment = provider.GetRequiredService<IHostEnvironment>();
            var connectionString = configuration["Communication:AzureCommunicationServicesConnectionString"];
            var mailpitBaseUrl = configuration["Communication:MailpitBaseUrl"];
            if (!environment.IsEnvironment("LocalKubernetes") && mailpitBaseUrl is not null)
                throw new InvalidOperationException("Communication:MailpitBaseUrl is only permitted in LocalKubernetes.");

            if (environment.IsEnvironment("LocalKubernetes"))
            {
                if (!Uri.TryCreate(mailpitBaseUrl, UriKind.Absolute, out var mailpitUri)
                    || (mailpitUri.Scheme != Uri.UriSchemeHttp && mailpitUri.Scheme != Uri.UriSchemeHttps))
                    throw new InvalidOperationException("Communication:MailpitBaseUrl is required and must be an absolute HTTP URL.");
                if (!NotificationFailures.IsValidRecipient(configuration["Communication:SenderAddress"]))
                    throw new InvalidOperationException("Communication:SenderAddress must be a valid email address.");
                if (!Uri.TryCreate(configuration["Communication:ExternalBaseUrl"], UriKind.Absolute, out _))
                    throw new InvalidOperationException("Communication:ExternalBaseUrl is required and must be an absolute URL.");

                var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(MailpitEmailSender.HttpClientName);
                client.BaseAddress = mailpitUri;
                return new MailpitEmailSender(client, configuration["Communication:SenderAddress"]!);
            }

            if (environment.IsDevelopment() && string.IsNullOrWhiteSpace(connectionString))
                return ActivatorUtilities.CreateInstance<LoggingEmailSender>(provider);

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Communication:AzureCommunicationServicesConnectionString is required.");
            if (!NotificationFailures.IsValidRecipient(configuration["Communication:SenderAddress"]))
                throw new InvalidOperationException("Communication:SenderAddress must be a valid email address.");
            if (!environment.IsDevelopment()
                && !Uri.TryCreate(configuration["Communication:ExternalBaseUrl"], UriKind.Absolute, out _))
                throw new InvalidOperationException("Communication:ExternalBaseUrl is required and must be an absolute URL.");

            return new AzureCommunicationEmailSender(configuration);
        });
        services.AddHostedService<EmailTransportStartup>();
        return services;
    }
}
