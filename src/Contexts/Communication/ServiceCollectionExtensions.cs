using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CatCar.Contexts.Communication.Infrastructure;
using FluentValidation;

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

        services.Configure<CommunicationOptions>(configuration.GetSection(CommunicationOptions.SectionName));

        // FluentValidation: registers IValidator<T> for every AbstractValidator<T> in this assembly.
        services.AddValidatorsFromAssemblyContaining<Marker>();

        // RiseOn.AutoInject: discovers [InjectService] attributed classes (repositories, ACL, email sender).
        services.UseCommunication();

        return services;
    }
}
