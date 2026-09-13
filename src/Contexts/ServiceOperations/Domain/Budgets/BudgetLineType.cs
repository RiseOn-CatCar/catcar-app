namespace CatCar.Contexts.ServiceOperations.Domain.Budgets;

/// <summary>
/// Whether a <see cref="BudgetLine"/> refers to a service (CatalogedService) or a part/supply (InventoryItem).
/// </summary>
public enum BudgetLineType
{
    Service,
    Part
}
