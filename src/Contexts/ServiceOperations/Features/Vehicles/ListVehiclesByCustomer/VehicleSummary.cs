namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.ListVehiclesByCustomer;

/// <summary>
/// Read model for a vehicle list item.
/// </summary>
public sealed record VehicleSummary(Guid Id, string Plate, string Brand, string Model, int ManufactureYear, bool IsActive);
