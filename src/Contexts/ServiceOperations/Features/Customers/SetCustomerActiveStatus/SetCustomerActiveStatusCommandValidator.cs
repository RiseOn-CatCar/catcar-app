namespace CatCar.Contexts.ServiceOperations.Features.Customers.SetCustomerActiveStatus;

using FluentValidation;

public sealed class SetCustomerActiveStatusCommandValidator : AbstractValidator<SetCustomerActiveStatusCommand>
{
    public SetCustomerActiveStatusCommandValidator()
    {
        RuleFor(c => c.CustomerId).NotEmpty().WithMessage("O identificador do cliente é obrigatório.");
    }
}
