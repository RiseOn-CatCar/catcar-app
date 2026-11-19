namespace CatCar.Contexts.IdentityAccess.Infrastructure.Seed;

using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using CatCar.Contexts.IdentityAccess.Features.AdministrativeUsers.RegisterAdministrativeUser;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Seeds a default administrative user for local development, avoiding a chicken-and-egg
/// problem where the registration endpoint itself requires an Administrador to be already logged in.
/// Credentials are simulated data for the fictional CatCar workshop and MUST NOT be used in production.
/// </summary>
public static class AdministrativeUserSeeder
{
    public const string DefaultAdminEmail = "admin@catcar.dev.br";
    public const string DefaultAdminPassword = "Catcar@Admin123";

    public static async Task EnsureAdministratorAsync(IServiceProvider rootServices, string email, string password, CancellationToken cancellationToken = default)
    {
        var command = new RegisterAdministrativeUserCommand("CatCar Administrator", email, password, nameof(AdministrativeRole.Administrador));
        var validation = await new RegisterAdministrativeUserCommandValidator().ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new InvalidOperationException("Invalid bootstrap administrator configuration.");

        var emailResult = Email.Create(email);
        if (emailResult.IsFailure)
            throw new InvalidOperationException("Invalid bootstrap administrator email.");

        var scope = rootServices.CreateAsyncScope();
        try
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAdministrativeUserRepository>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var existing = await repository.GetByEmailAsync(emailResult.Value, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                if (!existing.IsActive || existing.Role != AdministrativeRole.Administrador || !passwordHasher.Verify(password, existing.PasswordHash))
                    throw new InvalidOperationException("Existing bootstrap administrator credentials or role are invalid.");
                return;
            }

            var user = AdministrativeUser.Register(command.Name, email, passwordHasher.Hash(password), AdministrativeRole.Administrador);
            if (user.IsFailure)
                throw new InvalidOperationException("Invalid bootstrap administrator configuration.");
            await repository.AddAsync(user.Value, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
    }

    public static async Task SeedDefaultAdministratorAsync(IServiceProvider rootServices, CancellationToken cancellationToken = default)
    {
        var scope = rootServices.CreateAsyncScope();
        try
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAdministrativeUserRepository>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var emailResult = Email.Create(DefaultAdminEmail);
            if (emailResult.IsFailure)
                return;

            if (await repository.ExistsByEmailAsync(emailResult.Value, cancellationToken).ConfigureAwait(false))
                return;

            var passwordHash = passwordHasher.Hash(DefaultAdminPassword);
            var userResult = AdministrativeUser.Register("Administrador CatCar", DefaultAdminEmail, passwordHash, AdministrativeRole.Administrador);
            if (userResult.IsFailure)
                return;

            await repository.AddAsync(userResult.Value, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
    }
}
