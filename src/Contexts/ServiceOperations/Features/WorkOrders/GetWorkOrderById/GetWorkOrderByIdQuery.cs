namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderById;

/// <summary>
/// Query to fetch a single WorkOrder by its identifier, including requested services/parts.
/// </summary>
public sealed record GetWorkOrderByIdQuery(Guid WorkOrderId);
