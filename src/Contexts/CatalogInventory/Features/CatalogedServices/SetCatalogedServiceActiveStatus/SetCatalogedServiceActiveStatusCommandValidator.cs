namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.SetCatalogedServiceActiveStatus;

using FluentValidation;

public sealed class SetCatalogedServiceActiveStatusCommandValidator : AbstractValidator<SetCatalogedServiceActiveStatusCommand>
{
    public SetCatalogedServiceActiveStatusCommandValidator()
    {
        RuleFor(c => c.ServiceId)
            .NotEmpty().WithMessage("O identificador do serviço é obrigatório.");
    }
}
