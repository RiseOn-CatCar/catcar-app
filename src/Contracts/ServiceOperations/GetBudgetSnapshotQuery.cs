namespace CatCar.Contracts.ServiceOperations;

/// <summary>
/// Published-language query used by other bounded contexts (e.g. Communication) to obtain a read-only
/// snapshot of a Budget - including the customer's contact details needed to address the approval-link
/// e-mail - without depending on the ServiceOperations assembly. Dispatched in-process via Wolverine's
/// message bus (feature 05).
/// </summary>
public sealed record GetBudgetSnapshotQuery(Guid BudgetId);
