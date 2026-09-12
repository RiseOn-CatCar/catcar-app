namespace CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;

/// <summary>
/// Persistence port for <see cref="AdministrativeUser"/> aggregates.
/// Implemented in Infrastructure and registered via RiseOn.AutoInject.
/// </summary>
public interface IAdministrativeUserRepository
{
    Task<AdministrativeUser?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task AddAsync(AdministrativeUser user, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
