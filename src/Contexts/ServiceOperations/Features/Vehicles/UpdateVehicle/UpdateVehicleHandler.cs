namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.UpdateVehicle;

using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="UpdateVehicleCommand"/>.
/// </summary>
public static class UpdateVehicleHandler
{
    public static async Task<Upshot<UpdateVehicleResult>> Handle(
        UpdateVehicleCommand command,
        IValidator<UpdateVehicleCommand> validator,
        IVehicleRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<UpdateVehicleResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var vehicle = await repository.GetByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (vehicle is null)
            return Upshot<UpdateVehicleResult>.Fail("Veículo não encontrado.");

        var updateResult = vehicle.UpdateDetails(command.Brand, command.Model, command.ManufactureYear);
        if (updateResult.IsFailure)
            return Upshot<UpdateVehicleResult>.Fail(updateResult.Error);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<UpdateVehicleResult>.Success(
            new UpdateVehicleResult(vehicle.Id, vehicle.CustomerId, vehicle.Plate.Value, vehicle.Brand, vehicle.Model, vehicle.ManufactureYear, vehicle.IsActive));
    }
}
