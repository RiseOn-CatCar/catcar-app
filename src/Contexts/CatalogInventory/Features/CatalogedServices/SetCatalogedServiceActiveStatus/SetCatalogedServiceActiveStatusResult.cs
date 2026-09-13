namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.SetCatalogedServiceActiveStatus;

/// <summary>
/// Result of a successful cataloged service status change.
/// </summary>
public sealed record SetCatalogedServiceActiveStatusResult(Guid Id, bool IsActive);
