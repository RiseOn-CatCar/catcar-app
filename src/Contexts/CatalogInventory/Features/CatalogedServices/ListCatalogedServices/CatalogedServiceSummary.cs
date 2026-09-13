namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.ListCatalogedServices;

/// <summary>
/// Read model for a cataloged service list item.
/// </summary>
public sealed record CatalogedServiceSummary(Guid Id, string Name, int EstimatedDurationMinutes, decimal Price, bool IsActive);
