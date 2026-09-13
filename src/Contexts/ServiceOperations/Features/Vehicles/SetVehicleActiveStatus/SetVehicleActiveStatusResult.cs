namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.SetVehicleActiveStatus;

/// <summary>
/// Result of a successful vehicle status change.
/// </summary>
public sealed record SetVehicleActiveStatusResult(Guid Id, bool IsActive);
