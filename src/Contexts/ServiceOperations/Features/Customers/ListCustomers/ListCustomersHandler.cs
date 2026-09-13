namespace CatCar.Contexts.ServiceOperations.Features.Customers.ListCustomers;

using CatCar.Contexts.ServiceOperations.Domain.Customers;

/// <summary>
/// Wolverine handler for <see cref="ListCustomersQuery"/>.
/// </summary>
public static class ListCustomersHandler
{
    public static async Task<IReadOnlyList<CustomerSummary>> Handle(
        ListCustomersQuery query,
        ICustomerRepository repository,
        CancellationToken cancellationToken)
    {
        var customers = await repository.ListAsync(query.OnlyActive, cancellationToken).ConfigureAwait(false);

        return customers
            .Select(c => new CustomerSummary(c.Id, c.Document.ToString(), c.Name, c.Phone, c.IsActive))
            .ToList();
    }
}
