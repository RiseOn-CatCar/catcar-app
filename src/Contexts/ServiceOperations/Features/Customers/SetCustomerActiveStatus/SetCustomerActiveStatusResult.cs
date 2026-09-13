namespace CatCar.Contexts.ServiceOperations.Features.Customers.SetCustomerActiveStatus;

/// <summary>
/// Result of a successful customer status change.
/// </summary>
public sealed record SetCustomerActiveStatusResult(Guid Id, bool IsActive);
