namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.ListWorkOrders;

/// <summary>
/// Read model for a WorkOrder list item.
/// </summary>
public sealed record WorkOrderSummary(Guid Id, Guid CustomerId, Guid VehicleId, string Status, Guid? ActiveBudgetId, DateTime OpenedAt);
