namespace CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;

public interface IBudgetReservationOutcomeRepository
{
    Task<BudgetReservationOutcome?> GetByBudgetIdAsync(Guid budgetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetReservationOutcome>> GetByBudgetIdsAsync(IReadOnlyList<Guid> budgetIds, CancellationToken cancellationToken = default);
    Task AddAsync(BudgetReservationOutcome outcome, CancellationToken cancellationToken = default);
}
