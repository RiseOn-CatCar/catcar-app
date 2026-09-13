namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderById;

/// <summary>
/// Read model for a single WorkOrder, including its requested services/parts lines.
/// </summary>
public sealed record WorkOrderDetails(
    Guid Id,
    Guid CustomerId,
    Guid VehicleId,
    string InitialDescription,
    string Status,
    Guid? ActiveBudgetId,
    DateTime OpenedAt,
    IReadOnlyList<RequestedServiceLineDetails> RequestedServices,
    IReadOnlyList<RequestedPartLineDetails> RequestedParts);

/// <summary>
/// Read model for a requested service line.
/// </summary>
public sealed record RequestedServiceLineDetails(Guid Id, Guid CatalogedServiceId, string Description, decimal UnitPrice, int Quantity, decimal LineTotal);

/// <summary>
/// Read model for a requested part line.
/// </summary>
public sealed record RequestedPartLineDetails(Guid Id, Guid InventoryItemId, string Description, decimal UnitPrice, int Quantity, decimal LineTotal);
