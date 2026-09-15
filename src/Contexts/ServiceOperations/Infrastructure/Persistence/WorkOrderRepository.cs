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

    public async Task<IReadOnlyList<WorkOrder>> ListAsync(Guid? customerId, WorkOrderStatus? status, bool includeClosed, CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkOrders
            .Include(w => w.RequestedServices)
            .Include(w => w.RequestedParts)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(w => w.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);
        else if (!includeClosed)
            query = query.Where(w => w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Delivered);

        return await query
            .OrderByDescending(w => w.Status == WorkOrderStatus.InExecution)
            .ThenByDescending(w => w.Status == WorkOrderStatus.AwaitingApproval)
            .ThenByDescending(w => w.Status == WorkOrderStatus.InDiagnosis)
            .ThenByDescending(w => w.Status == WorkOrderStatus.Received)
            .ThenBy(w => w.OpenedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WorkOrderExecutionTimeMetrics> GetAverageExecutionTimeAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkOrders
            .Where(w => (w.Status == WorkOrderStatus.Completed || w.Status == WorkOrderStatus.Delivered) && w.CompletedAt.HasValue)
            .GroupBy(_ => 1)
            .Select(group => new WorkOrderExecutionTimeMetrics(
                group.Count(),
                group.Average(w => (w.CompletedAt!.Value - w.OpenedAt).TotalHours),
                group.Average(w => w.DiagnosisStartedAt.HasValue && w.BudgetApprovedAt.HasValue
                    ? (double?)(w.BudgetApprovedAt!.Value - w.DiagnosisStartedAt!.Value).TotalHours
                    : null) ?? 0,
                group.Average(w => w.BudgetApprovedAt.HasValue
                    ? (double?)(w.CompletedAt!.Value - w.BudgetApprovedAt!.Value).TotalHours
                    : null) ?? 0))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? WorkOrderExecutionTimeMetrics.Empty;
    }

    public Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        dbContext.WorkOrders.Add(workOrder);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
