namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.IssueBudget;

/// <summary>
/// Result of a successful budget issuance.
/// </summary>
public sealed record IssueBudgetResult(Guid BudgetId, Guid WorkOrderId, string Status, decimal TotalAmount, DateTime IssuedAt, IReadOnlyList<IssueBudgetLineResult> Lines);

/// <summary>
/// A single frozen line of the issued budget.
/// </summary>
public sealed record IssueBudgetLineResult(string Type, Guid ReferenceId, string Description, decimal UnitPrice, int Quantity, decimal LineTotal);
