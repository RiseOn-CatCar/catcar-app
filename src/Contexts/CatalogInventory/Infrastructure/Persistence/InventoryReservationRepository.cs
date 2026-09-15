namespace CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;

using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using Microsoft.EntityFrameworkCore;
using RiseOn.AutoInject;

[InjectService(ServiceLifetimeType.Scoped, CollectionName = "CatalogInventory")]
public sealed class InventoryReservationRepository(CatalogInventoryDbContext dbContext) : IInventoryReservationRepository
{
    public async Task<IReadOnlyList<InventoryReservation>> GetActiveReservationsByWorkOrderIdAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        return await dbContext.InventoryReservations
            .Where(r => r.WorkOrderId == workOrderId && r.Status == InventoryReservationStatus.Reserved)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<InventoryReservation>> GetActiveReservationsByBudgetIdAsync(Guid budgetId, CancellationToken cancellationToken = default)
    {
        return await dbContext.InventoryReservations
            .Where(r => r.BudgetId == budgetId && r.Status == InventoryReservationStatus.Reserved)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(InventoryReservation reservation, CancellationToken cancellationToken = default)
    {
        await dbContext.InventoryReservations.AddAsync(reservation, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddRangeAsync(IEnumerable<InventoryReservation> reservations, CancellationToken cancellationToken = default)
    {
        await dbContext.InventoryReservations.AddRangeAsync(reservations, cancellationToken).ConfigureAwait(false);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
