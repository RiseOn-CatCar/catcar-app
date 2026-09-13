namespace CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;
using RiseOn.AutoInject;

/// <summary>
/// EF Core implementation of <see cref="IWorkOrderRepository"/>.
/// Registered via RiseOn.AutoInject into the ServiceOperations DI collection.
/// </summary>
[InjectService(ServiceLifetimeType.Scoped, CollectionName = "ServiceOperations")]
public sealed class WorkOrderRepository(ServiceOperationsDbContext dbContext) : IWorkOrderRepository
{
    public Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.WorkOrders
            .Include(w => w.RequestedServices)
            .Include(w => w.RequestedParts)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkOrder>> ListAsync(Guid? customerId, WorkOrderStatus? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkOrders
            .Include(w => w.RequestedServices)
            .Include(w => w.RequestedParts)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(w => w.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);

        return await query.OrderByDescending(w => w.OpenedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        dbContext.WorkOrders.Add(workOrder);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
