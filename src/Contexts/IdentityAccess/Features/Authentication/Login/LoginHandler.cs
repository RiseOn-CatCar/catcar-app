namespace CatCar.Contexts.IdentityAccess.Features.Authentication.Login;

using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="LoginCommand"/>.
/// Discovered by Wolverine's static-method handler convention; dependencies are method-injected from DI.
/// </summary>
public static class LoginHandler
{
    public static async Task<Upshot<LoginResult>> Handle(
        LoginCommand command,
        IValidator<LoginCommand> validator,
        IAdministrativeUserRepository repository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<LoginResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
            return Upshot<LoginResult>.Fail("Credenciais inválidas.");

        var user = await repository.GetByEmailAsync(emailResult.Value, cancellationToken).ConfigureAwait(false);
        if (user is null || !user.IsActive || !passwordHasher.Verify(command.Password, user.PasswordHash))
            return Upshot<LoginResult>.Fail("Credenciais inválidas.");

        var token = tokenGenerator.Generate(user);

        return Upshot<LoginResult>.Success(new LoginResult(token.AccessToken, token.ExpiresAtUtc, user.Name, user.Role.ToString()));
    }
}
