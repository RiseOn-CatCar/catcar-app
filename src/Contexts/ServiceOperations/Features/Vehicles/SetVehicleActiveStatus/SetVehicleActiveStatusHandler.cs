namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.SetVehicleActiveStatus;

using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="SetVehicleActiveStatusCommand"/>.
/// </summary>
public static class SetVehicleActiveStatusHandler
{
    public static async Task<Upshot<SetVehicleActiveStatusResult>> Handle(
        SetVehicleActiveStatusCommand command,
        IValidator<SetVehicleActiveStatusCommand> validator,
        IVehicleRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<SetVehicleActiveStatusResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var vehicle = await repository.GetByIdAsync(command.VehicleId, cancellationToken).ConfigureAwait(false);
        if (vehicle is null)
            return Upshot<SetVehicleActiveStatusResult>.Fail("Veículo não encontrado.");

        vehicle.SetActiveStatus(command.IsActive);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<SetVehicleActiveStatusResult>.Success(new SetVehicleActiveStatusResult(vehicle.Id, vehicle.IsActive));
    }
}
