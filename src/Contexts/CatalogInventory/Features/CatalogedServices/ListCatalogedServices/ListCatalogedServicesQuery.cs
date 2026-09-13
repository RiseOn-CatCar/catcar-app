namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.ListCatalogedServices;

/// <summary>
/// Query to list cataloged services, optionally filtered by active status.
/// </summary>
public sealed record ListCatalogedServicesQuery(bool? OnlyActive);
