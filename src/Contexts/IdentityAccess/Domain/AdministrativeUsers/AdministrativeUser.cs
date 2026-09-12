namespace CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;

using CatCar.SharedKernel;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Aggregate root representing an administrative panel access credential.
/// AC: autenticação JWT para APIs administrativas, papéis/perfis básicos (feature 02).
/// Aliases (forbidden per glossary): "cliente", "funcionário".
/// </summary>
public sealed class AdministrativeUser : Entity<Guid>, IAggregateRoot
{
    public Email Email { get; private set; }

    public string Name { get; private set; }

    public string PasswordHash { get; private set; }

    public AdministrativeRole Role { get; private set; }

    public bool IsActive { get; private set; }

    private AdministrativeUser(Guid id, Email email, string name, string passwordHash, AdministrativeRole role)
        : base(id)
    {
        Email = email;
        Name = name;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
    }

    /// <summary>
    /// Registers a new administrative user with a role already assigned.
    /// The password must already be hashed by an <see cref="IPasswordHasher"/> before reaching the aggregate;
    /// the domain never handles plain-text passwords directly.
    /// </summary>
    public static Upshot<AdministrativeUser> Register(string? name, string? email, string passwordHash, AdministrativeRole role)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Upshot<AdministrativeUser>.Fail("O nome é obrigatório.");

        if (name.Trim().Length > 150)
            return Upshot<AdministrativeUser>.Fail("O nome deve ter no máximo 150 caracteres.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Upshot<AdministrativeUser>.Fail("A senha é obrigatória.");

        var emailResult = Email.Create(email);
        if (emailResult.IsFailure)
            return Upshot<AdministrativeUser>.Fail(emailResult.Error);

        var user = new AdministrativeUser(Guid.CreateVersion7(), emailResult.Value, name.Trim(), passwordHash, role);
        return Upshot<AdministrativeUser>.Success(user);
    }

    /// <summary>
    /// Deactivates the administrative user, revoking future login capability.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            throw new BusinessRuleViolatedException("O usuário administrativo já está inativo.");

        IsActive = false;
    }

    /// <summary>
    /// Reactivates a previously deactivated administrative user.
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            throw new BusinessRuleViolatedException("O usuário administrativo já está ativo.");

        IsActive = true;
    }

    /// <summary>
    /// Changes the role/profile assigned to the administrative user.
    /// </summary>
    public void ChangeRole(AdministrativeRole newRole)
    {
        Role = newRole;
    }

    /// <summary>
    /// Replaces the stored password hash (e.g. after a password reset flow).
    /// </summary>
    public void ChangePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new BusinessRuleViolatedException("A nova senha é obrigatória.");

        PasswordHash = newPasswordHash;
    }
}
