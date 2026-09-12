namespace CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;

/// <summary>
/// Domain port for password hashing. Implemented with Argon2id in Infrastructure.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password into a storable representation (salt + hash).
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Verifies a plain-text password against a previously stored hash.
    /// </summary>
    bool Verify(string password, string passwordHash);
}
