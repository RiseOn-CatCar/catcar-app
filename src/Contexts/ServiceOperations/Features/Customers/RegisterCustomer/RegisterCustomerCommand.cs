namespace CatCar.Contexts.ServiceOperations.Features.Customers.RegisterCustomer;

/// <summary>
/// Command to register a new customer identified by CPF/CNPJ.
/// </summary>
public sealed record RegisterCustomerCommand(string Document, string Name, string Phone, string? Email);
