namespace CatCar.Contexts.Communication.Infrastructure.Persistence;

using CatCar.Contexts.Communication.Domain.ExternalAccessTokens;
using Microsoft.EntityFrameworkCore;
using RiseOn.AutoInject;

/// <summary>
/// EF Core implementation of <see cref="IExternalAccessTokenRepository"/>.
/// Registered via RiseOn.AutoInject into the Communication DI collection.
/// </summary>
[InjectService(ServiceLifetimeType.Scoped, CollectionName = "Communication")]
public sealed class ExternalAccessTokenRepository(CommunicationDbContext dbContext) : IExternalAccessTokenRepository
{
    public Task<ExternalAccessToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.ExternalAccessTokens.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<ExternalAccessToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => dbContext.ExternalAccessTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public Task<ExternalAccessToken?> GetActiveByBudgetIdAsync(Guid budgetId, CancellationToken cancellationToken = default)
        => dbContext.ExternalAccessTokens
            .Where(t => t.BudgetId == budgetId && t.Status == ExternalAccessTokenStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

    public Task AddAsync(ExternalAccessToken token, CancellationToken cancellationToken = default)
    {
        dbContext.ExternalAccessTokens.Add(token);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
