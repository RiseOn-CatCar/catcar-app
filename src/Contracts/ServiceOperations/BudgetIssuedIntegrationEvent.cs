using CatCar.SharedKernel;

namespace CatCar.Contracts.ServiceOperations;

/// <summary>
/// Integration event raised when a budget has been issued.
/// </summary>
public record BudgetIssuedIntegrationEvent : IIntegrationEvent
{
}
