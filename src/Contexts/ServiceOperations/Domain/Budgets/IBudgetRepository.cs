namespace CatCar.Contexts.ServiceOperations.Domain.Budgets;

/// <summary>
/// Persistence port for <see cref="Budget"/> aggregates.
/// Implemented in Infrastructure and registered via RiseOn.AutoInject.
/// </summary>
public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Budget?> GetActiveByWorkOrderIdAsync(Guid workOrderId, CancellationToken cancellationToken = default);

    Task AddAsync(Budget budget, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
