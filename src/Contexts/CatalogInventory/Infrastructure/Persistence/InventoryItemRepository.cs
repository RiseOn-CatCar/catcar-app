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

    public async Task<Upshot> ReserveAsync(
        IReadOnlyCollection<InventoryReservation> reservations,
        CancellationToken cancellationToken = default)
    {
        if (reservations.Count == 0)
            return Upshot.Fail("At least one inventory reservation is required.");

        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var reservationGroup in reservations.GroupBy(r => r.InventoryItemId).OrderBy(g => g.Key))
            {
                var quantity = reservationGroup.Sum(r => r.Quantity);
                var affectedRows = await dbContext.InventoryItems
                    .Where(i => i.Id == reservationGroup.Key && i.QuantityInStock >= quantity)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(i => i.QuantityInStock, i => i.QuantityInStock - quantity),
                        cancellationToken)
                    .ConfigureAwait(false);

                if (affectedRows != 1)
                    return Upshot.Fail($"Insufficient stock for inventory item {reservationGroup.Key}.");
            }

            await dbContext.InventoryReservations.AddRangeAsync(reservations, cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Upshot.Success();
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task<Upshot> ReleaseReservationsAsync(
        IReadOnlyCollection<InventoryReservation> reservations,
        DateTime releasedAt,
        CancellationToken cancellationToken = default)
    {
        if (reservations.Count == 0)
            return Upshot.Success();

        if (reservations.Any(r => r.Status != InventoryReservationStatus.Reserved))
            return Upshot.Fail("Only reserved inventory can be released.");

        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var reservationGroup in reservations.GroupBy(r => r.InventoryItemId).OrderBy(g => g.Key))
            {
                var quantity = reservationGroup.Sum(r => r.Quantity);
                var affectedRows = await dbContext.InventoryItems
                    .Where(i => i.Id == reservationGroup.Key)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(i => i.QuantityInStock, i => i.QuantityInStock + quantity),
                        cancellationToken)
                    .ConfigureAwait(false);

                if (affectedRows != 1)
                    return Upshot.Fail($"Inventory item {reservationGroup.Key} was not found while releasing stock.");
            }

            foreach (var reservation in reservations)
            {
                var releaseResult = reservation.Release(releasedAt);
                if (releaseResult.IsFailure)
                    return Upshot.Fail(releaseResult.Error);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Upshot.Success();
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task<Upshot> ConsumeReservationsAsync(
        IReadOnlyCollection<InventoryReservation> reservations,
        DateTime consumedAt,
        CancellationToken cancellationToken = default)
    {
        if (reservations.Count == 0)
            return Upshot.Success();

        if (reservations.Any(r => r.Status != InventoryReservationStatus.Reserved))
            return Upshot.Fail("Only reserved inventory can be consumed.");

        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var reservation in reservations)
            {
                var consumeResult = reservation.MarkAsConsumed(consumedAt);
                if (consumeResult.IsFailure)
                    return Upshot.Fail(consumeResult.Error);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Upshot.Success();
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
