namespace CatCar.Contexts.IdentityAccess.Features.AdministrativeUsers.RegisterAdministrativeUser;

/// <summary>
/// Command to register a new administrative user with an assigned role.
/// Role is a string (e.g. "Administrador", "Recepcionista", "Tecnico") validated against the AdministrativeRole enum.
/// </summary>
public sealed record RegisterAdministrativeUserCommand(string Name, string Email, string Password, string Role);
