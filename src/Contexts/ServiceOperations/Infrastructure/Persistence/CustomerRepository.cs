namespace CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using RiseOn.AutoInject;

/// <summary>
/// EF Core implementation of <see cref="ICustomerRepository"/>.
/// Registered via RiseOn.AutoInject into the ServiceOperations DI collection.
/// </summary>
[InjectService(ServiceLifetimeType.Scoped, CollectionName = "ServiceOperations")]
public sealed class CustomerRepository(ServiceOperationsDbContext dbContext) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Customer?> GetByDocumentAsync(string documentDigits, CancellationToken cancellationToken = default)
        => dbContext.Customers.FirstOrDefaultAsync(c => c.Document.Value == documentDigits, cancellationToken);

    public Task<bool> ExistsByDocumentAsync(string documentDigits, CancellationToken cancellationToken = default)
        => dbContext.Customers.AnyAsync(c => c.Document.Value == documentDigits, cancellationToken);

    public async Task<IReadOnlyList<Customer>> ListAsync(bool? onlyActive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Customers.AsQueryable();

        if (onlyActive.HasValue)
            query = query.Where(c => c.IsActive == onlyActive.Value);

        return await query.OrderBy(c => c.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        dbContext.Customers.Add(customer);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
