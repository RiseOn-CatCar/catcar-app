namespace CatCar.Contracts.CatalogInventory;

public sealed record GetBudgetReservationOutcomesQuery(IReadOnlyList<Guid> BudgetIds);

public sealed record BudgetReservationOutcomeResponse(Guid BudgetId, string Outcome, string? FailureCode, DateTime ResolvedAt);
