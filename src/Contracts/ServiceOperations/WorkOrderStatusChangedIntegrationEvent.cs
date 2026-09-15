using CatCar.SharedKernel;

namespace CatCar.Contracts.ServiceOperations;

/// <summary>
/// Integration event raised when a work order status has changed.
/// </summary>
public sealed record WorkOrderStatusChangedIntegrationEvent(
    Guid WorkOrderId,
    Guid CustomerId,
    string PreviousStatus,
    string NewStatus,
    DateTime ChangedAt,
    Guid CorrelationId) : IIntegrationEvent;
