namespace CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerByDocument;

/// <summary>
/// Query to fetch a single customer by CPF/CNPJ document.
/// </summary>
public sealed record GetCustomerByDocumentQuery(string Document);
