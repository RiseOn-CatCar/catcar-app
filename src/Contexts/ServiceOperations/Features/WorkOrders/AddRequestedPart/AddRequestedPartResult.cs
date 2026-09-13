namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedPart;

/// <summary>
/// Result of successfully including a requested part/supply on a WorkOrder.
/// </summary>
public sealed record AddRequestedPartResult(Guid WorkOrderId, Guid InventoryItemId, string Description, decimal UnitPrice, int Quantity, decimal LineTotal);
