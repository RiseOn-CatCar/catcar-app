namespace CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;

using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using RiseOn.AutoInject;

/// <summary>
/// EF Core implementation of <see cref="IVehicleRepository"/>.
/// Registered via RiseOn.AutoInject into the ServiceOperations DI collection.
/// </summary>
[InjectService(ServiceLifetimeType.Scoped, CollectionName = "ServiceOperations")]
public sealed class VehicleRepository(ServiceOperationsDbContext dbContext) : IVehicleRepository
{
    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<bool> ExistsByPlateAsync(string plate, CancellationToken cancellationToken = default)
        => dbContext.Vehicles.AnyAsync(v => v.Plate.Value == plate, cancellationToken);

    public async Task<IReadOnlyList<Vehicle>> ListByCustomerAsync(Guid customerId, bool? onlyActive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Vehicles.Where(v => v.CustomerId == customerId);

        if (onlyActive.HasValue)
            query = query.Where(v => v.IsActive == onlyActive.Value);

        return await query.OrderBy(v => v.Brand).ThenBy(v => v.Model).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        dbContext.Vehicles.Add(vehicle);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
