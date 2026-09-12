namespace CatCar.Contexts.IdentityAccess.Features.AdministrativeUsers.RegisterAdministrativeUser;

using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="RegisterAdministrativeUserCommand"/>.
/// </summary>
public static class RegisterAdministrativeUserHandler
{
    public static async Task<Upshot<RegisterAdministrativeUserResult>> Handle(
        RegisterAdministrativeUserCommand command,
        IValidator<RegisterAdministrativeUserCommand> validator,
        IAdministrativeUserRepository repository,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<RegisterAdministrativeUserResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
            return Upshot<RegisterAdministrativeUserResult>.Fail(emailResult.Error);

        if (await repository.ExistsByEmailAsync(emailResult.Value, cancellationToken).ConfigureAwait(false))
            return Upshot<RegisterAdministrativeUserResult>.Fail("Já existe um usuário administrativo com este e-mail.");

        var role = Enum.Parse<AdministrativeRole>(command.Role, ignoreCase: true);
        var passwordHash = passwordHasher.Hash(command.Password);

        var userResult = AdministrativeUser.Register(command.Name, command.Email, passwordHash, role);
        if (userResult.IsFailure)
            return Upshot<RegisterAdministrativeUserResult>.Fail(userResult.Error);

        var user = userResult.Value;

        await repository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<RegisterAdministrativeUserResult>.Success(
            new RegisterAdministrativeUserResult(user.Id, user.Name, user.Email.Value, user.Role.ToString()));
    }
}
