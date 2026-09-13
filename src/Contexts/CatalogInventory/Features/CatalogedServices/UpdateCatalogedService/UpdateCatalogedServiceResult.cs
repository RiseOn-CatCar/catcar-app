namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.UpdateCatalogedService;

/// <summary>
/// Result of a successful cataloged service update.
/// </summary>
public sealed record UpdateCatalogedServiceResult(Guid Id, string Name, string Description, int EstimatedDurationMinutes, decimal Price, bool IsActive);
