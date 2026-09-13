namespace CatCar.Contexts.ServiceOperations.Domain.Customers;

/// <summary>
/// Persistence port for <see cref="Customer"/> aggregates.
/// Implemented in Infrastructure and registered via RiseOn.AutoInject.
/// </summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Customer?> GetByDocumentAsync(string documentDigits, CancellationToken cancellationToken = default);

    Task<bool> ExistsByDocumentAsync(string documentDigits, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> ListAsync(bool? onlyActive, CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
