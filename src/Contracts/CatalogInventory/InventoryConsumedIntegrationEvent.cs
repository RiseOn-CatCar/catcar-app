using CatCar.SharedKernel;

namespace CatCar.Contracts.CatalogInventory;

/// <summary>
/// Integration event raised when inventory has been consumed.
/// </summary>
public record InventoryConsumedIntegrationEvent : IIntegrationEvent
{
}
