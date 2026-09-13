namespace CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerById;

/// <summary>
/// Read model for a single customer.
/// </summary>
public sealed record CustomerDetails(Guid Id, string Document, string Name, string Phone, string? Email, bool IsActive);
