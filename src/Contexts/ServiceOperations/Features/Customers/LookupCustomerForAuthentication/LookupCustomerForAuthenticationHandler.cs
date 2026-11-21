namespace CatCar.Contexts.ServiceOperations.Features.Customers.LookupCustomerForAuthentication;

using CatCar.Contexts.ServiceOperations.Domain.Customers;

public sealed record AuthenticationCustomer(Guid Id, bool IsActive);

public static class LookupCustomerForAuthenticationHandler
{
    public static async Task<AuthenticationCustomer?> Handle(string documentNumber, ICustomerRepository repository, CancellationToken cancellationToken)
    {
        var customer = await repository.GetByDocumentAsync(documentNumber, cancellationToken).ConfigureAwait(false);
        return customer is null ? null : new AuthenticationCustomer(customer.Id, customer.IsActive);
    }
}
