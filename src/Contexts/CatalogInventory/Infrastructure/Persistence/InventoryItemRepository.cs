namespace CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using Microsoft.EntityFrameworkCore;
using RiseOn.AutoInject;
using RiseOn.RailResult.Upshot;

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

    public async Task<InventoryReservationFailureCode?> PrepareReservationAsync(
        IReadOnlyCollection<InventoryReservation> reservations, CancellationToken cancellationToken = default)
    {
        var items = new List<(InventoryItem Item, int Quantity)>();
        foreach (var group in reservations.GroupBy(r => r.InventoryItemId).OrderBy(g => g.Key))
        {
            var quantity = group.Sum(r => r.Quantity);
            var item = await GetByIdAsync(group.Key, cancellationToken).ConfigureAwait(false);
            if (item is null) return InventoryReservationFailureCode.InventoryItemNotFound;
            if (!item.IsActive) return InventoryReservationFailureCode.InventoryItemInactive;
            if (quantity <= 0) return InventoryReservationFailureCode.InvalidBudgetLines;
            if (item.QuantityInStock < quantity) return InventoryReservationFailureCode.InsufficientStock;
            items.Add((item, quantity));
        }
        foreach (var (item, quantity) in items)
            item.AdjustStock(StockMovementType.Saida, quantity);
        await dbContext.InventoryReservations.AddRangeAsync(reservations, cancellationToken).ConfigureAwait(false);
        return null;
    }

    public async Task<Upshot> PrepareReleaseAsync(
        IReadOnlyCollection<InventoryReservation> reservations, DateTime releasedAt, CancellationToken cancellationToken = default)
    {
        if (reservations.Any(r => r.Status != InventoryReservationStatus.Reserved))
            return Upshot.Fail("Only reserved inventory can be released.");
        var items = new List<(InventoryItem Item, int Quantity)>();
        foreach (var group in reservations.GroupBy(r => r.InventoryItemId).OrderBy(g => g.Key))
        {
            var item = await GetByIdAsync(group.Key, cancellationToken).ConfigureAwait(false);
            if (item is null) return Upshot.Fail($"Inventory item {group.Key} was not found while releasing stock.");
            items.Add((item, group.Sum(r => r.Quantity)));
        }
        foreach (var (item, quantity) in items)
            item.AdjustStock(StockMovementType.Entrada, quantity);
        foreach (var reservation in reservations)
        {
            var result = reservation.Release(releasedAt);
            if (result.IsFailure) return Upshot.Fail(result.Error);
        }
        return Upshot.Success();
    }

    public Task<Upshot> PrepareConsumptionAsync(
        IReadOnlyCollection<InventoryReservation> reservations, DateTime consumedAt, CancellationToken cancellationToken = default)
    {
        if (reservations.Any(r => r.Status != InventoryReservationStatus.Reserved))
            return Task.FromResult(Upshot.Fail("Only reserved inventory can be consumed."));
        foreach (var reservation in reservations)
        {
            var result = reservation.MarkAsConsumed(consumedAt);
            if (result.IsFailure) return Task.FromResult(Upshot.Fail(result.Error));
        }
        return Task.FromResult(Upshot.Success());
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
