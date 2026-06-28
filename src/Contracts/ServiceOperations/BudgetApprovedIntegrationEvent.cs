using CatCar.SharedKernel;

namespace CatCar.Contracts.ServiceOperations;

/// <summary>
/// Integration event raised when a budget has been approved.
/// </summary>
public record BudgetApprovedIntegrationEvent : IIntegrationEvent
{
}
