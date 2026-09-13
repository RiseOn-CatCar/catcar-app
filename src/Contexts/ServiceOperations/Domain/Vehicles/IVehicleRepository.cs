namespace CatCar.Contexts.ServiceOperations.Domain.Vehicles;

/// <summary>
/// Persistence port for <see cref="Vehicle"/> aggregates.
/// Implemented in Infrastructure and registered via RiseOn.AutoInject.
/// </summary>
public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByPlateAsync(string plate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehicle>> ListByCustomerAsync(Guid customerId, bool? onlyActive, CancellationToken cancellationToken = default);

    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
