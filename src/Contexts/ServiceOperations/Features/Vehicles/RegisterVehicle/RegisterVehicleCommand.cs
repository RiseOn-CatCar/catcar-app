namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.RegisterVehicle;

/// <summary>
/// Command to register a new vehicle for an existing customer.
/// </summary>
public sealed record RegisterVehicleCommand(Guid CustomerId, string Plate, string Brand, string Model, int ManufactureYear);
