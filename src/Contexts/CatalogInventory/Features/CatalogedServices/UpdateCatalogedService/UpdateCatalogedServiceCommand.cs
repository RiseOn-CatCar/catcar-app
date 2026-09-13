namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.UpdateCatalogedService;

/// <summary>
/// Command to update the descriptive and pricing details of an existing cataloged service.
/// </summary>
public sealed record UpdateCatalogedServiceCommand(Guid ServiceId, string Name, string Description, int EstimatedDurationMinutes, decimal Price);
