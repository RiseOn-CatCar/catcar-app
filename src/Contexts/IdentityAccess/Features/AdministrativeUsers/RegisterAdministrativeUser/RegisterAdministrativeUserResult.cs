namespace CatCar.Contexts.IdentityAccess.Features.AdministrativeUsers.RegisterAdministrativeUser;

/// <summary>
/// Result of a successful administrative user registration.
/// </summary>
public sealed record RegisterAdministrativeUserResult(Guid Id, string Name, string Email, string Role);
