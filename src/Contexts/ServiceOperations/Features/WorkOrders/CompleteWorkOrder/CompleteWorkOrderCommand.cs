namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.CompleteWorkOrder;

public sealed record CompleteWorkOrderCommand(Guid WorkOrderId, Guid CorrelationId);
