namespace CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;

using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using Microsoft.EntityFrameworkCore;
using RiseOn.AutoInject;

[InjectService(ServiceLifetimeType.Scoped, CollectionName = "CatalogInventory")]
public sealed class BudgetReservationOutcomeRepository(CatalogInventoryDbContext dbContext) : IBudgetReservationOutcomeRepository
{
    public Task<BudgetReservationOutcome?> GetByBudgetIdAsync(Guid budgetId, CancellationToken cancellationToken = default)
        => dbContext.BudgetReservationOutcomes.FirstOrDefaultAsync(x => x.BudgetId == budgetId, cancellationToken);

    public async Task<IReadOnlyList<BudgetReservationOutcome>> GetByBudgetIdsAsync(
        IReadOnlyList<Guid> budgetIds, CancellationToken cancellationToken = default)
    {
        var results = new List<BudgetReservationOutcome>();
        foreach (var batch in budgetIds.Distinct().Chunk(100))
        {
            var outcomes = await dbContext.BudgetReservationOutcomes.AsNoTracking()
                .Where(x => batch.Contains(x.BudgetId)).ToListAsync(cancellationToken).ConfigureAwait(false);
            results.AddRange(outcomes);
        }
        return results;
    }

    public async Task AddAsync(BudgetReservationOutcome outcome, CancellationToken cancellationToken = default)
    {
        await dbContext.BudgetReservationOutcomes.AddAsync(outcome, cancellationToken).ConfigureAwait(false);
    }
}
