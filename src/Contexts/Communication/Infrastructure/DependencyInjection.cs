using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddSingleton<LoggingEmailSender>();
        services.AddSingleton<IEmailSender, AzureCommunicationEmailSender>();
        return services;
    }
}
