namespace CatCar.Contexts.ServiceOperations.Features.Customers.RegisterCustomer;

/// <summary>
/// Result of a successful customer registration.
/// </summary>
public sealed record RegisterCustomerResult(Guid Id, string Document, string Name, string Phone, string? Email, bool IsActive);
