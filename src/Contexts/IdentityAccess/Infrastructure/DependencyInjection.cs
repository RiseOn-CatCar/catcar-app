using System.Text;
using CatCar.Contexts.IdentityAccess.Infrastructure.Persistence;
using CatCar.Contexts.IdentityAccess.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

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

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        var customerJwtSigningKey = configuration["CustomerJwt:SigningKey"] ?? jwtSettings.Secret;
        var issuerSigningKeys = new[] { jwtSettings.Secret, customerJwtSigningKey }
            .Where(static signingKey => !string.IsNullOrWhiteSpace(signingKey))
            .Distinct(StringComparer.Ordinal)
            .Select(static signingKey => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)))
            .Cast<SecurityKey>()
            .ToArray();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuers = [jwtSettings.Issuer, "CatCar"],
                    ValidAudiences = [jwtSettings.Audience, "CatCar.Api", "CatCar.Customer"],
                    IssuerSigningKeys = issuerSigningKeys,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization();

        return services;
    }
}
