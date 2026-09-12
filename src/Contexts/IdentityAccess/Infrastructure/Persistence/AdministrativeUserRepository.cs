namespace CatCar.Contexts.IdentityAccess.Infrastructure.Persistence;

using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using Microsoft.EntityFrameworkCore;
using RiseOn.AutoInject;

/// <summary>
/// EF Core implementation of <see cref="IAdministrativeUserRepository"/>.
/// Registered via RiseOn.AutoInject into the IdentityAccess DI collection.
/// </summary>
[InjectService(ServiceLifetimeType.Scoped, CollectionName = "IdentityAccess")]
public sealed class AdministrativeUserRepository(IdentityAccessDbContext dbContext) : IAdministrativeUserRepository
{
    public Task<AdministrativeUser?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        => dbContext.AdministrativeUsers.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
        => dbContext.AdministrativeUsers.AnyAsync(u => u.Email == email, cancellationToken);

    public Task AddAsync(AdministrativeUser user, CancellationToken cancellationToken = default)
    {
        dbContext.AdministrativeUsers.Add(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
