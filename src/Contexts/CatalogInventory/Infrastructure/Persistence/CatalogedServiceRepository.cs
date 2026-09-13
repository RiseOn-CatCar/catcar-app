namespace CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;

using CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;
using Microsoft.EntityFrameworkCore;
using RiseOn.AutoInject;

/// <summary>
/// EF Core implementation of <see cref="ICatalogedServiceRepository"/>.
/// Registered via RiseOn.AutoInject into the CatalogInventory DI collection.
/// </summary>
[InjectService(ServiceLifetimeType.Scoped, CollectionName = "CatalogInventory")]
public sealed class CatalogedServiceRepository(CatalogInventoryDbContext dbContext) : ICatalogedServiceRepository
{
    public Task<CatalogedService?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.CatalogedServices.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        => dbContext.CatalogedServices.AnyAsync(s => s.Name == name, cancellationToken);

    public async Task<IReadOnlyList<CatalogedService>> ListAsync(bool? onlyActive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.CatalogedServices.AsQueryable();

        if (onlyActive.HasValue)
            query = query.Where(s => s.IsActive == onlyActive.Value);

        return await query.OrderBy(s => s.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task AddAsync(CatalogedService service, CancellationToken cancellationToken = default)
    {
        dbContext.CatalogedServices.Add(service);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
