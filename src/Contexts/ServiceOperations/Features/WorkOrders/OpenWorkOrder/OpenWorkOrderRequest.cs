namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.OpenWorkOrder;

public sealed record OpenWorkOrderRequest(
    Guid CustomerId,
    Guid VehicleId,
    string InitialDescription,
    IReadOnlyList<OpenWorkOrderItemDto>? Services = null,
    IReadOnlyList<OpenWorkOrderItemDto>? Parts = null);
