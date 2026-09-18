namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.OpenWorkOrder;

/// <summary>
/// Command to open a new WorkOrder (OS) for an identified customer and vehicle.
/// </summary>
public sealed record OpenWorkOrderCommand(
    Guid CustomerId,
    Guid VehicleId,
    string InitialDescription,
    IReadOnlyList<OpenWorkOrderItemDto>? Services = null,
    IReadOnlyList<OpenWorkOrderItemDto>? Parts = null,
    Guid CorrelationId = default);
