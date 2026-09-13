namespace CatCar.Contracts.ServiceOperations;

/// <summary>
/// Published-language response to <see cref="RecordBudgetDecisionCommand"/>.
/// </summary>
public sealed record RecordBudgetDecisionResponse(
    bool Success,
    string? Error,
    Guid BudgetId,
    Guid WorkOrderId,
    string BudgetStatus,
    string WorkOrderStatus);
