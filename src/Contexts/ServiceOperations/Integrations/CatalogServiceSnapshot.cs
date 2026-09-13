namespace CatCar.Contexts.ServiceOperations.Integrations;

/// <summary>
/// Local (ACL-translated) read model describing a CatalogedService, used when including a requested
/// service on a WorkOrder. Never exposes CatalogInventory's own domain types (per ACL boundary rule).
/// </summary>
public sealed record CatalogServiceSnapshot(Guid Id, string Description, decimal UnitPrice, bool IsActive);

/// <summary>
/// Local (ACL-translated) read model describing an InventoryItem, used when including a requested
/// part/supply on a WorkOrder. Never exposes CatalogInventory's own domain types (per ACL boundary rule).
/// </summary>
public sealed record InventoryItemSnapshot(Guid Id, string Description, decimal UnitPrice, bool IsActive, int QuantityInStock);
