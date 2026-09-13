namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.GetCatalogedServiceById;

/// <summary>
/// Query to fetch a single cataloged service by its identifier.
/// </summary>
public sealed record GetCatalogedServiceByIdQuery(Guid ServiceId);
