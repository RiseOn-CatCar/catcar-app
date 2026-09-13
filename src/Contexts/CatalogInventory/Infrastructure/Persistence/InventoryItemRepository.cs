namespace CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using Microsoft.EntityFrameworkCore;
using RiseOn.AutoInject;

/// <summary>
/// EF Core implementation of <see cref="IInventoryItemRepository"/>.
/// Registered via RiseOn.AutoInject into the CatalogInventory DI collection.
/// </summary>
[InjectService(ServiceLifetimeType.Scoped, CollectionName = "CatalogInventory")]
public sealed class InventoryItemRepository(CatalogInventoryDbContext dbContext) : IInventoryItemRepository
{
    public Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.InventoryItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default)
        => dbContext.InventoryItems.AnyAsync(i => i.Sku == sku, cancellationToken);

    public async Task<IReadOnlyList<InventoryItem>> ListAsync(bool? onlyActive, bool? onlyBelowMinimumStock, CancellationToken cancellationToken = default)
    {
        var query = dbContext.InventoryItems.AsQueryable();

        if (onlyActive.HasValue)
            query = query.Where(i => i.IsActive == onlyActive.Value);

        if (onlyBelowMinimumStock.HasValue && onlyBelowMinimumStock.Value)
            query = query.Where(i => i.QuantityInStock < i.MinimumStockThreshold);

        return await query.OrderBy(i => i.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        dbContext.InventoryItems.Add(item);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
