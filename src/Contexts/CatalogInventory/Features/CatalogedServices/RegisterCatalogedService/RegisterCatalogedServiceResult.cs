namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.RegisterCatalogedService;

/// <summary>
/// Result of a successful cataloged service registration.
/// </summary>
public sealed record RegisterCatalogedServiceResult(Guid Id, string Name, string Description, int EstimatedDurationMinutes, decimal Price, bool IsActive);
