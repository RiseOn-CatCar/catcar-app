namespace CatCar.Contexts.IdentityAccess.Features.Authentication.Login;

/// <summary>
/// Result of a successful administrative login.
/// </summary>
public sealed record LoginResult(string AccessToken, DateTime ExpiresAtUtc, string Name, string Role);
