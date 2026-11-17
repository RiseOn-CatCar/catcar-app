using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace CatCar.Contexts.CatalogInventory.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogInventoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextWithWolverineIntegration<CatalogInventoryDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("catcar"));
            options.AddInterceptors(new AuditInterceptor());
        }, wolverineDatabaseSchema: "wolverine");
        return services;
    }
}
