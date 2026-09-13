namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.RegisterVehicle;

/// <summary>
/// Result of a successful vehicle registration.
/// </summary>
public sealed record RegisterVehicleResult(Guid Id, Guid CustomerId, string Plate, string Brand, string Model, int ManufactureYear, bool IsActive);
