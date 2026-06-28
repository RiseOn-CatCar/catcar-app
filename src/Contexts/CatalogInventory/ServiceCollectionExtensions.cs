using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CatCar.Contexts.CatalogInventory.Infrastructure;

namespace CatCar.Contexts.CatalogInventory;

/// <summary>
/// Service registration for the CatalogInventory Bounded Context.
/// Uses RiseOn.AutoInject for automatic handler/validator discovery.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers CatalogInventory Bounded Context services using RiseOn.AutoInject.
    /// Auto-injected services are discovered from [AutoInject] attributed classes.
    /// </summary>
    public static IServiceCollection AddCatalogInventory(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCatalogInventoryInfrastructure(configuration);

        // RiseOn.AutoInject: uncomment when [InjectService] attributed classes exist
        // services.UseCatalogInventory();

        return services;
    }
}
