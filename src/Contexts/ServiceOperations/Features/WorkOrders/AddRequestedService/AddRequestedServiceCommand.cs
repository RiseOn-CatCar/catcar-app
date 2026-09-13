namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedService;

/// <summary>
/// Command to include a requested service on an existing WorkOrder, taking a price/description
/// snapshot from the CatalogInventory context via the ACL.
/// </summary>
public sealed record AddRequestedServiceCommand(Guid WorkOrderId, Guid CatalogedServiceId, int Quantity);
