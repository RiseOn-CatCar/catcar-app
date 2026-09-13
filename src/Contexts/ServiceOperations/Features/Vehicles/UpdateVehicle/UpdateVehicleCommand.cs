namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.UpdateVehicle;

/// <summary>
/// Command to update a vehicle's brand/model/year. The plate is immutable.
/// </summary>
public sealed record UpdateVehicleCommand(Guid Id, string Brand, string Model, int ManufactureYear);
