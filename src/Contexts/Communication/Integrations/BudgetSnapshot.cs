namespace CatCar.Contexts.Communication.Integrations;

/// <summary>
/// Local (ACL-translated) read model describing a Budget and its customer's contact details, used when
/// issuing an approval link or rendering the customer-facing approval details. Never exposes
/// ServiceOperations' own domain types (per ACL boundary rule).
/// </summary>
public sealed record BudgetSnapshot(
    Guid BudgetId,
    Guid WorkOrderId,
    Guid CustomerId,
    string CustomerName,
    string? CustomerEmail,
    string BudgetStatus,
    decimal TotalAmount,
    DateTime IssuedAt,
    IReadOnlyList<BudgetSnapshotLine> Lines);

/// <summary>A single frozen line of a <see cref="BudgetSnapshot"/>.</summary>
public sealed record BudgetSnapshotLine(string Type, string Description, decimal UnitPrice, int Quantity, decimal LineTotal);
