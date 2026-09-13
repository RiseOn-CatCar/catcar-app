namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.ListVehiclesByCustomer;

/// <summary>
/// Query to list a customer's vehicles, optionally filtered by active status.
/// </summary>
public sealed record ListVehiclesByCustomerQuery(Guid CustomerId, bool? OnlyActive);
