using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using FluentValidation;
using CatCar.Contexts.ServiceOperations.Integrations;
using CatCar.Contexts.ServiceOperations.Features.Customers.LookupCustomerForAuthentication;

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
        services.Configure<InternalApiOptions>(configuration.GetSection("InternalApi"));

        // FluentValidation: registers IValidator<T> for every AbstractValidator<T> in this assembly.
        services.AddValidatorsFromAssemblyContaining<Marker>();

        // RiseOn.AutoInject: discovers [InjectService] attributed classes (repositories, ACLs).
        services.UseServiceOperations();
        services.AddHttpClient<ICatalogInventoryAcl, CatalogInventoryAcl>(client =>
            client.BaseAddress = new Uri(configuration["Services:Execution"] ?? "https+http://execution-service"));

        return services;
    }
}
