namespace CatCar.Contexts.IdentityAccess.Features.Authentication.Login;

using FluentValidation;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.");

        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("A senha é obrigatória.");
    }
}
