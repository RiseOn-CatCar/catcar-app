namespace CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerById;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="GetCustomerByIdQuery"/>.
/// </summary>
public static class GetCustomerByIdHandler
{
    public static async Task<Upshot<CustomerDetails>> Handle(
        GetCustomerByIdQuery query,
        ICustomerRepository repository,
        CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(query.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
            return Upshot<CustomerDetails>.Fail("Cliente não encontrado.");

        return Upshot<CustomerDetails>.Success(
            new CustomerDetails(customer.Id, customer.Document.ToString(), customer.Name, customer.Phone, customer.Email, customer.IsActive));
    }
}
