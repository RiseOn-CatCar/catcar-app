using CatCar.Contexts.IdentityAccess.Infrastructure;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // FluentValidation: registers IValidator<T> for every AbstractValidator<T> in this assembly.
        services.AddValidatorsFromAssemblyContaining<Marker>();

        // RiseOn.AutoInject: discovers [InjectService] attributed classes (repository, password hasher, token generator).
        services.UseIdentityAccess();

        return services;
    }
}
