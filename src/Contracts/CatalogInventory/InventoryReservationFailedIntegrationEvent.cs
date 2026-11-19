using CatCar.SharedKernel;

namespace CatCar.Contracts.CatalogInventory;

public sealed record InventoryReservationFailedIntegrationEvent(
    Guid WorkOrderId, Guid BudgetId, string FailureCode, DateTime FailedAt, Guid CorrelationId) : IIntegrationEvent;
