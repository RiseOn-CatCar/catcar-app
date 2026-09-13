namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.GetVehicleById;

using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="GetVehicleByIdQuery"/>.
/// </summary>
public static class GetVehicleByIdHandler
{
    public static async Task<Upshot<VehicleDetails>> Handle(
        GetVehicleByIdQuery query,
        IVehicleRepository repository,
        CancellationToken cancellationToken)
    {
        var vehicle = await repository.GetByIdAsync(query.VehicleId, cancellationToken).ConfigureAwait(false);
        if (vehicle is null)
            return Upshot<VehicleDetails>.Fail("Veículo não encontrado.");

        return Upshot<VehicleDetails>.Success(
            new VehicleDetails(vehicle.Id, vehicle.CustomerId, vehicle.Plate.Value, vehicle.Brand, vehicle.Model, vehicle.ManufactureYear, vehicle.IsActive));
    }
}
