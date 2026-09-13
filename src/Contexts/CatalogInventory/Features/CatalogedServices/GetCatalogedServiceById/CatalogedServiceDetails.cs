namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.GetCatalogedServiceById;

/// <summary>
/// Read model for a single cataloged service.
/// </summary>
public sealed record CatalogedServiceDetails(Guid Id, string Name, string Description, int EstimatedDurationMinutes, decimal Price, bool IsActive);
