namespace CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;

/// <summary>
/// Roles available for administrative panel access.
/// AC: papéis/perfis básicos (feature 02 - identidade-acesso-administrativo).
/// </summary>
public enum AdministrativeRole
{
    /// <summary>
    /// Full administrative access: manages administrative users, catalog and configuration.
    /// </summary>
    Administrador = 0,

    /// <summary>
    /// Front-desk access: customer/vehicle intake, work order opening and budget communication.
    /// </summary>
    Recepcionista = 1,

    /// <summary>
    /// Workshop access: work order execution, status updates and evidence registration.
    /// </summary>
    Tecnico = 2,
}
