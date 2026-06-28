using CatCar.SharedKernel;

namespace CatCar.Contracts.ServiceOperations;

/// <summary>
/// Integration event raised when a work order status has changed.
/// </summary>
public record WorkOrderStatusChangedIntegrationEvent : IIntegrationEvent
{
}
