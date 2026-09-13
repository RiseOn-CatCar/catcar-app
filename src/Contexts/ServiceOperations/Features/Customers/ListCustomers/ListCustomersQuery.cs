namespace CatCar.Contexts.ServiceOperations.Features.Customers.ListCustomers;

/// <summary>
/// Query to list customers, optionally filtered by active status.
/// </summary>
public sealed record ListCustomersQuery(bool? OnlyActive);
