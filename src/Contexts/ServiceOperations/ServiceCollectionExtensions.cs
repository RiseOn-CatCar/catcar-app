using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CatCar.Contexts.ServiceOperations.Infrastructure;

namespace CatCar.Contexts.ServiceOperations;

/// <summary>
/// Service registration for the ServiceOperations Bounded Context.
/// Uses RiseOn.AutoInject for automatic handler/validator discovery.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ServiceOperations Bounded Context services using RiseOn.AutoInject.
    /// Auto-injected services are discovered from [AutoInject] attributed classes.
    /// </summary>
    public static IServiceCollection AddServiceOperations(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceOperationsInfrastructure(configuration);

        // RiseOn.AutoInject: uncomment when [InjectService] attributed classes exist
        // services.UseServiceOperations();

        return services;
    }
}
