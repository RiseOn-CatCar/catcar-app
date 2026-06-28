using CatCar.SharedKernel;

namespace CatCar.Contracts.CatalogInventory;

/// <summary>
/// Integration event raised when inventory has been reserved.
/// </summary>
public record InventoryReservedIntegrationEvent : IIntegrationEvent
{
}
