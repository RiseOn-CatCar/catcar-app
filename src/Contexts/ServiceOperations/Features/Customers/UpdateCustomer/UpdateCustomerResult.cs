namespace CatCar.Contexts.ServiceOperations.Features.Customers.UpdateCustomer;

/// <summary>
/// Result of a successful customer update.
/// </summary>
public sealed record UpdateCustomerResult(Guid Id, string Document, string Name, string Phone, string? Email, bool IsActive);
