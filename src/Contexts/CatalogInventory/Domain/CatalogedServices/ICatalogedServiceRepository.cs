namespace CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;

/// <summary>
/// Persistence port for <see cref="CatalogedService"/> aggregates.
/// Implemented in Infrastructure and registered via RiseOn.AutoInject.
/// </summary>
public interface ICatalogedServiceRepository
{
    Task<CatalogedService?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogedService>> ListAsync(bool? onlyActive, CancellationToken cancellationToken = default);

    Task AddAsync(CatalogedService service, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
