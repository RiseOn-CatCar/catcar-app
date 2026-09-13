namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.SetVehicleActiveStatus;

using FluentValidation;

public sealed class SetVehicleActiveStatusCommandValidator : AbstractValidator<SetVehicleActiveStatusCommand>
{
    public SetVehicleActiveStatusCommandValidator()
    {
        RuleFor(c => c.VehicleId).NotEmpty().WithMessage("O identificador do veículo é obrigatório.");
    }
}
