namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.ListVehiclesByCustomer;

using CatCar.Contexts.ServiceOperations.Domain.Vehicles;

/// <summary>
/// Wolverine handler for <see cref="ListVehiclesByCustomerQuery"/>.
/// </summary>
public static class ListVehiclesByCustomerHandler
{
    public static async Task<IReadOnlyList<VehicleSummary>> Handle(
        ListVehiclesByCustomerQuery query,
        IVehicleRepository repository,
        CancellationToken cancellationToken)
    {
        var vehicles = await repository.ListByCustomerAsync(query.CustomerId, query.OnlyActive, cancellationToken).ConfigureAwait(false);

        return vehicles
            .Select(v => new VehicleSummary(v.Id, v.Plate.Value, v.Brand, v.Model, v.ManufactureYear, v.IsActive))
            .ToList();
    }
}
