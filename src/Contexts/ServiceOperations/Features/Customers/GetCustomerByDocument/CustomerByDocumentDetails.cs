namespace CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerByDocument;

/// <summary>
/// Read model for a single customer looked up by document.
/// </summary>
public sealed record CustomerByDocumentDetails(Guid Id, string Document, string Name, string Phone, string? Email, bool IsActive);
