namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.GetVehicleById;

/// <summary>
/// Read model for a single vehicle.
/// </summary>
public sealed record VehicleDetails(Guid Id, Guid CustomerId, string Plate, string Brand, string Model, int ManufactureYear, bool IsActive);
