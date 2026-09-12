namespace CatCar.Contexts.IdentityAccess.Features.Authentication.Login;

/// <summary>
/// Command to authenticate an administrative user and obtain a JWT access token.
/// </summary>
public sealed record LoginCommand(string Email, string Password);
