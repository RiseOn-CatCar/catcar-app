namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedPart;

/// <summary>
/// Command to include a requested part/supply on an existing WorkOrder, taking a price/description/stock
/// snapshot from the CatalogInventory context via the ACL.
/// </summary>
public sealed record AddRequestedPartCommand(Guid WorkOrderId, Guid InventoryItemId, int Quantity);
