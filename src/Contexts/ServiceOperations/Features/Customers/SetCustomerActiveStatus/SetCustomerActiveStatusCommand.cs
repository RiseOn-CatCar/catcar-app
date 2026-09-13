namespace CatCar.Contexts.ServiceOperations.Features.Customers.SetCustomerActiveStatus;

/// <summary>
/// Command to activate or deactivate a customer.
/// </summary>
public sealed record SetCustomerActiveStatusCommand(Guid CustomerId, bool IsActive);
