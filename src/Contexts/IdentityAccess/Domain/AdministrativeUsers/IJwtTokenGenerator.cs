namespace CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;

/// <summary>
/// Domain port for issuing JWT access tokens for administrative users.
/// Implemented in Infrastructure using the app's signing configuration.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a signed access token carrying the user's identity and role claims.
    /// </summary>
    GeneratedToken Generate(AdministrativeUser user);
}

/// <summary>
/// A signed access token and its UTC expiration instant.
/// </summary>
public sealed record GeneratedToken(string AccessToken, DateTime ExpiresAtUtc);
