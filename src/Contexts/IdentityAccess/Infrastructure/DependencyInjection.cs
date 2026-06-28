using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CatCar.Contexts.IdentityAccess.Infrastructure.Persistence;

namespace CatCar.Contexts.IdentityAccess.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityAccessInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityAccessDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("catcar"));
            options.AddInterceptors(new AuditInterceptor());
        });
        return services;
    }
}
