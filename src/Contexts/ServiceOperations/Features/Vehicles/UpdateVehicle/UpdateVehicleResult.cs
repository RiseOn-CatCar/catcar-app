namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.UpdateVehicle;

/// <summary>
/// Result of a successful vehicle update.
/// </summary>
public sealed record UpdateVehicleResult(Guid Id, Guid CustomerId, string Plate, string Brand, string Model, int ManufactureYear, bool IsActive);
