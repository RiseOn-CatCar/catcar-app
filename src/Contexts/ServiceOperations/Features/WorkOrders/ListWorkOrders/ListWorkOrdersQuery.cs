namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.ListWorkOrders;

/// <summary>
/// Query to list WorkOrders, optionally filtered by customer and/or status.
/// </summary>
public sealed record ListWorkOrdersQuery(Guid? CustomerId, string? Status);
