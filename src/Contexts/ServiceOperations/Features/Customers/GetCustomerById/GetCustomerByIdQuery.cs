namespace CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerById;

/// <summary>
/// Query to fetch a single customer by its identifier.
/// </summary>
public sealed record GetCustomerByIdQuery(Guid CustomerId);
