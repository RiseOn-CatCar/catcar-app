namespace CatCar.Contexts.ServiceOperations.Domain.WorkOrders;

/// <summary>
/// Persistence port for <see cref="WorkOrder"/> aggregates.
/// Implemented in Infrastructure and registered via RiseOn.AutoInject.
/// </summary>
public interface IWorkOrderRepository
{
    Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkOrder>> ListAsync(Guid? customerId, WorkOrderStatus? status, bool includeClosed, CancellationToken cancellationToken = default);

    Task<WorkOrderExecutionTimeMetrics> GetAverageExecutionTimeAsync(CancellationToken cancellationToken = default);

    Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
