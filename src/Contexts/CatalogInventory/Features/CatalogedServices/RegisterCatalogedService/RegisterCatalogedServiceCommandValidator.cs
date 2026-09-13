namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.RegisterCatalogedService;

using FluentValidation;

public sealed class RegisterCatalogedServiceCommandValidator : AbstractValidator<RegisterCatalogedServiceCommand>
{
    public RegisterCatalogedServiceCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("O nome do serviço é obrigatório.")
            .MaximumLength(150).WithMessage("O nome do serviço deve ter no máximo 150 caracteres.");

        RuleFor(c => c.Description)
            .NotEmpty().WithMessage("A descrição do serviço é obrigatória.")
            .MaximumLength(500).WithMessage("A descrição do serviço deve ter no máximo 500 caracteres.");

        RuleFor(c => c.EstimatedDurationMinutes)
            .GreaterThan(0).WithMessage("A duração estimada deve ser maior que zero minutos.");

        RuleFor(c => c.Price)
            .GreaterThanOrEqualTo(0).WithMessage("O preço do serviço não pode ser negativo.");
    }
}
