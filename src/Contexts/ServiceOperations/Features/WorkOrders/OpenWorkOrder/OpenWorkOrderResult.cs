namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.OpenWorkOrder;

/// <summary>
/// Result of a successful WorkOrder opening.
/// </summary>
public sealed record OpenWorkOrderResult(Guid Id, Guid CustomerId, Guid VehicleId, string InitialDescription, string Status, DateTime OpenedAt);
