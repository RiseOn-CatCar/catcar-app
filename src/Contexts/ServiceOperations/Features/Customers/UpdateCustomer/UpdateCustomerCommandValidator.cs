namespace CatCar.Contexts.ServiceOperations.Features.Customers.UpdateCustomer;

using FluentValidation;

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty().WithMessage("O identificador do cliente é obrigatório.");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("O nome do cliente é obrigatório.")
            .MaximumLength(150).WithMessage("O nome deve ter no máximo 150 caracteres.");

        RuleFor(c => c.Phone)
            .NotEmpty().WithMessage("O telefone do cliente é obrigatório.")
            .MaximumLength(20).WithMessage("O telefone deve ter no máximo 20 caracteres.");

        RuleFor(c => c.Email)
            .MaximumLength(200).WithMessage("O e-mail deve ter no máximo 200 caracteres.")
            .EmailAddress().WithMessage("O e-mail informado é inválido.")
            .When(c => !string.IsNullOrWhiteSpace(c.Email));
    }
}
