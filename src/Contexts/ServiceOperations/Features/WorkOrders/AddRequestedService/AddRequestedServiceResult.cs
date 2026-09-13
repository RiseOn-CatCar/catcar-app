namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedService;

/// <summary>
/// Result of successfully including a requested service on a WorkOrder.
/// </summary>
public sealed record AddRequestedServiceResult(Guid WorkOrderId, Guid CatalogedServiceId, string Description, decimal UnitPrice, int Quantity, decimal LineTotal);
