namespace CatCar.Contexts.ServiceOperations.Integrations;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contracts.ServiceOperations;

/// <summary>
/// Wolverine handler answering the published-language <see cref="GetCustomerSnapshotQuery"/> defined in
/// <c>CatCar.Contracts.ServiceOperations</c>.
/// </summary>
public static class CustomerSnapshotQueryHandler
{
    public static async Task<CustomerSnapshotResponse> Handle(
        GetCustomerSnapshotQuery query,
        ICustomerRepository customerRepository,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(query.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
            return NotFound(query.CustomerId);

        return new CustomerSnapshotResponse(
            Found: true,
            CustomerId: customer.Id,
            Name: customer.Name,
            Email: customer.Email,
            Phone: customer.Phone,
            IsActive: customer.IsActive);
    }

    private static CustomerSnapshotResponse NotFound(Guid customerId) =>
        new(false, customerId, string.Empty, null, null, false);
}
