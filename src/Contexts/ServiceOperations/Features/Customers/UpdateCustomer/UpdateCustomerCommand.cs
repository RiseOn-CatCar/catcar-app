namespace CatCar.Contexts.ServiceOperations.Features.Customers.UpdateCustomer;

/// <summary>
/// Command to update a customer's contact details. The document number is immutable.
/// </summary>
public sealed record UpdateCustomerCommand(Guid Id, string Name, string Phone, string? Email);
