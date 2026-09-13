namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.SetVehicleActiveStatus;

/// <summary>
/// Command to activate or deactivate a vehicle.
/// </summary>
public sealed record SetVehicleActiveStatusCommand(Guid VehicleId, bool IsActive);
