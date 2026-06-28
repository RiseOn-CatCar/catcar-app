using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;

namespace CatCar.Contexts.ServiceOperations.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddServiceOperationsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ServiceOperationsDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("catcar"));
            options.AddInterceptors(new AuditInterceptor());
        });
        return services;
    }
}
