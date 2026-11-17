using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace CatCar.Contexts.ServiceOperations.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddServiceOperationsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextWithWolverineIntegration<ServiceOperationsDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("catcar"));
            options.AddInterceptors(new AuditInterceptor());
        }, wolverineDatabaseSchema: "wolverine");
        return services;
    }
}
