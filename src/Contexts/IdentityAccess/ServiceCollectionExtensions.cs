using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CatCar.Contexts.IdentityAccess.Infrastructure;

namespace CatCar.Contexts.IdentityAccess;

/// <summary>
/// Service registration for the IdentityAccess Bounded Context.
/// Uses RiseOn.AutoInject for automatic handler/validator discovery.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers IdentityAccess Bounded Context services using RiseOn.AutoInject.
    /// Auto-injected services are discovered from [AutoInject] attributed classes.
    /// </summary>
    public static IServiceCollection AddIdentityAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityAccessInfrastructure(configuration);

        // RiseOn.AutoInject: uncomment when [InjectService] attributed classes exist
        // services.UseIdentityAccess();

        return services;
    }
}
