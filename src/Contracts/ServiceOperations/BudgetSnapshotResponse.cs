namespace CatCar.Contracts.ServiceOperations;

/// <summary>
/// A single frozen line of a <see cref="BudgetSnapshotResponse"/>.
/// </summary>
public sealed record BudgetSnapshotLine(string Type, string Description, decimal UnitPrice, int Quantity, decimal LineTotal);

/// <summary>
/// Published-language response to <see cref="GetBudgetSnapshotQuery"/>.
/// </summary>
public sealed record BudgetSnapshotResponse(
    bool Found,
    Guid BudgetId,
    Guid WorkOrderId,
    Guid CustomerId,
    string CustomerName,
    string? CustomerEmail,
    string BudgetStatus,
    decimal TotalAmount,
    DateTime IssuedAt,
    IReadOnlyList<BudgetSnapshotLine> Lines);
