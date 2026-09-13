using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using FluentValidation;

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

        // FluentValidation: registers IValidator<T> for every AbstractValidator<T> in this assembly.
        services.AddValidatorsFromAssemblyContaining<Marker>();

        // RiseOn.AutoInject: discovers [InjectService] attributed classes (repositories, ACLs).
        services.UseServiceOperations();

        return services;
    }
}
