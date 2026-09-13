namespace CatCar.Contracts.ServiceOperations;

/// <summary>
/// Published-language command used by Communication to record a customer's approval or rejection decision
/// against a Budget/WorkOrder, once the external access token has already been validated on the caller's
/// side (feature 05). Dispatched in-process via Wolverine's message bus.
/// </summary>
public sealed record RecordBudgetDecisionCommand(Guid BudgetId, bool Approved, string? RejectionReason);
