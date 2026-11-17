namespace CatCar.Contexts.IdentityAccess.Tests.Integration;

using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using CatCar.Contexts.IdentityAccess.Infrastructure;
using CatCar.Contexts.IdentityAccess.Infrastructure.Persistence;
using CatCar.Contexts.IdentityAccess.Infrastructure.Security;
using CatCar.Contexts.IdentityAccess.Infrastructure.Seed;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class AdministrativeUserSeederTests : IntegrationTestBase
{
    [Fact]
    public async Task EnsureAdministratorAsync_RepeatedWithSameCredentials_PreservesHash()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new IdentityAccessDbContext(DbContextOptions));
        services.AddScoped<IAdministrativeUserRepository, AdministrativeUserRepository>();
        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
        await using var provider = services.BuildServiceProvider();
        await AdministrativeUserSeeder.EnsureAdministratorAsync(provider, "bootstrap@example.com", "StrongPassword123");
        await using var db = CreateDbContext();
        var first = await db.Set<AdministrativeUser>().AsNoTracking().SingleAsync();
        await AdministrativeUserSeeder.EnsureAdministratorAsync(provider, "bootstrap@example.com", "StrongPassword123");
        var second = await db.Set<AdministrativeUser>().AsNoTracking().SingleAsync();
        second.PasswordHash.Should().Be(first.PasswordHash);
        second.Role.Should().Be(AdministrativeRole.Administrador);
        var action = () => AdministrativeUserSeeder.EnsureAdministratorAsync(provider, "bootstrap@example.com", "WrongPassword123");
        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
