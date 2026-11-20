namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.IssueBudget;

/// <summary>
/// Requests asynchronous budget issuance for a diagnosed work order.
/// </summary>
public sealed record IssueBudgetCommand(Guid WorkOrderId, Guid CorrelationId);
