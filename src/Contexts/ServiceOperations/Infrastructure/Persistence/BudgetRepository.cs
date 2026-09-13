namespace CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;

using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using Microsoft.EntityFrameworkCore;
using RiseOn.AutoInject;

/// <summary>
/// EF Core implementation of <see cref="IBudgetRepository"/>.
/// Registered via RiseOn.AutoInject into the ServiceOperations DI collection.
/// </summary>
[InjectService(ServiceLifetimeType.Scoped, CollectionName = "ServiceOperations")]
public sealed class BudgetRepository(ServiceOperationsDbContext dbContext) : IBudgetRepository
{
    public Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Budgets.Include(b => b.Lines).FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Budget?> GetActiveByWorkOrderIdAsync(Guid workOrderId, CancellationToken cancellationToken = default)
        => dbContext.Budgets
            .Include(b => b.Lines)
            .Where(b => b.WorkOrderId == workOrderId && b.Status == BudgetStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

    public Task AddAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        dbContext.Budgets.Add(budget);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
