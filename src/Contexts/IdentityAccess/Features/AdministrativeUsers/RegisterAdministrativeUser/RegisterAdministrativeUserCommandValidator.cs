namespace CatCar.Contexts.IdentityAccess.Features.AdministrativeUsers.RegisterAdministrativeUser;

using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using FluentValidation;

public sealed class RegisterAdministrativeUserCommandValidator : AbstractValidator<RegisterAdministrativeUserCommand>
{
    public RegisterAdministrativeUserCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(150).WithMessage("O nome deve ter no máximo 150 caracteres.");

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("O e-mail informado é inválido.");

        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(8).WithMessage("A senha deve ter ao menos 8 caracteres.");

        RuleFor(c => c.Role)
            .NotEmpty().WithMessage("O papel é obrigatório.")
            .Must(role => Enum.TryParse<AdministrativeRole>(role, ignoreCase: true, out _))
            .WithMessage("Papel inválido. Valores aceitos: Administrador, Recepcionista, Tecnico.");
    }
}
