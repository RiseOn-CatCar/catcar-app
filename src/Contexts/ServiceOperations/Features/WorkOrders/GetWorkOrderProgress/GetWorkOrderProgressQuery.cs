namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderProgress;

public sealed record GetWorkOrderProgressQuery(Guid WorkOrderId, Guid CustomerId);
