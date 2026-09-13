namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.GetVehicleById;

/// <summary>
/// Query to fetch a single vehicle by its identifier.
/// </summary>
public sealed record GetVehicleByIdQuery(Guid VehicleId);
