namespace CatCar.Contexts.CatalogInventory.Features.InventoryReservations;

using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contracts.CatalogInventory;

public static class GetBudgetReservationOutcomesHandler
{
    public static async Task<IReadOnlyList<BudgetReservationOutcomeResponse>> Handle(
        GetBudgetReservationOutcomesQuery query, IBudgetReservationOutcomeRepository repository, CancellationToken cancellationToken)
    {
        var outcomes = await repository.GetByBudgetIdsAsync(query.BudgetIds, cancellationToken).ConfigureAwait(false);
        return outcomes.Select(x => new BudgetReservationOutcomeResponse(
            x.BudgetId, x.Outcome.ToString(), x.FailureCode?.ToString(), x.ResolvedAt)).ToList();
    }
}
