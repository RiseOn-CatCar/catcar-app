namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.OpenWorkOrder;

/// <summary>
/// Catalog or inventory item requested when opening a work order.
/// </summary>
public sealed record OpenWorkOrderItemDto(Guid Id, int Quantity);
