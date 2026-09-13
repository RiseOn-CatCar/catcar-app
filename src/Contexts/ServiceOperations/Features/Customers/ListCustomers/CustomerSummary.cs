namespace CatCar.Contexts.ServiceOperations.Features.Customers.ListCustomers;

/// <summary>
/// Read model for a customer list item.
/// </summary>
public sealed record CustomerSummary(Guid Id, string Document, string Name, string Phone, bool IsActive);
