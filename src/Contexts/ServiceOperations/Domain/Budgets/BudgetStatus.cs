namespace CatCar.Contexts.ServiceOperations.Domain.Budgets;

/// <summary>
/// Lifecycle status of a Budget. Active -> Approved | Rejected -> Replaced.
/// </summary>
public enum BudgetStatus
{
    Active,
    Approved,
    Rejected,
    Replaced
}
