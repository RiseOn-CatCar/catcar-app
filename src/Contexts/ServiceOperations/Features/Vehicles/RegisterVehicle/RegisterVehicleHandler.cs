namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.RegisterVehicle;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="RegisterVehicleCommand"/>.
/// </summary>
public static class RegisterVehicleHandler
{
    public static async Task<Upshot<RegisterVehicleResult>> Handle(
        RegisterVehicleCommand command,
        IValidator<RegisterVehicleCommand> validator,
        ICustomerRepository customerRepository,
        IVehicleRepository vehicleRepository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<RegisterVehicleResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var customer = await customerRepository.GetByIdAsync(command.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
            return Upshot<RegisterVehicleResult>.Fail("Cliente não encontrado.");

        var vehicleResult = Vehicle.Register(command.CustomerId, command.Plate, command.Brand, command.Model, command.ManufactureYear);
        if (vehicleResult.IsFailure)
            return Upshot<RegisterVehicleResult>.Fail(vehicleResult.Error);

        var vehicle = vehicleResult.Value;

        if (await vehicleRepository.ExistsByPlateAsync(vehicle.Plate.Value, cancellationToken).ConfigureAwait(false))
            return Upshot<RegisterVehicleResult>.Fail("Já existe um veículo cadastrado com esta placa.");

        await vehicleRepository.AddAsync(vehicle, cancellationToken).ConfigureAwait(false);
        await vehicleRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<RegisterVehicleResult>.Success(
            new RegisterVehicleResult(vehicle.Id, vehicle.CustomerId, vehicle.Plate.Value, vehicle.Brand, vehicle.Model, vehicle.ManufactureYear, vehicle.IsActive));
    }
}
