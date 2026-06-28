using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CatCar.Contexts.Communication.Infrastructure;

namespace CatCar.Contexts.Communication;

/// <summary>
/// Service registration for the Communication Bounded Context.
/// Uses RiseOn.AutoInject for automatic handler/validator discovery.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Communication Bounded Context services using RiseOn.AutoInject.
    /// Auto-injected services are discovered from [AutoInject] attributed classes.
    /// </summary>
    public static IServiceCollection AddCommunication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCommunicationInfrastructure(configuration);

        // RiseOn.AutoInject: uncomment when [InjectService] attributed classes exist
        // services.UseCommunication();

        return services;
    }
}
