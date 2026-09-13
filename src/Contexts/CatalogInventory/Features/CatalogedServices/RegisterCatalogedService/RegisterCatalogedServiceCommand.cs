namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.RegisterCatalogedService;

/// <summary>
/// Command to register a new service offered by the workshop.
/// </summary>
public sealed record RegisterCatalogedServiceCommand(string Name, string Description, int EstimatedDurationMinutes, decimal Price);
