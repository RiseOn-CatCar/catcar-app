namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.DeliverWorkOrder;

public sealed record DeliverWorkOrderCommand(Guid WorkOrderId, Guid CorrelationId);
