namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.UpdateVehicle;

using FluentValidation;

public sealed class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty().WithMessage("O identificador do veículo é obrigatório.");

        RuleFor(c => c.Brand)
            .NotEmpty().WithMessage("A marca do veículo é obrigatória.")
            .MaximumLength(60).WithMessage("A marca deve ter no máximo 60 caracteres.");

        RuleFor(c => c.Model)
            .NotEmpty().WithMessage("O modelo do veículo é obrigatório.")
            .MaximumLength(60).WithMessage("O modelo deve ter no máximo 60 caracteres.");

        RuleFor(c => c.ManufactureYear)
            .InclusiveBetween(1950, DateTime.UtcNow.Year + 1)
            .WithMessage($"O ano de fabricação deve estar entre 1950 e {DateTime.UtcNow.Year + 1}.");
    }
}
