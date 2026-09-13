namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.SetCatalogedServiceActiveStatus;

/// <summary>
/// Command to activate or deactivate a cataloged service.
/// </summary>
public sealed record SetCatalogedServiceActiveStatusCommand(Guid ServiceId, bool IsActive);
