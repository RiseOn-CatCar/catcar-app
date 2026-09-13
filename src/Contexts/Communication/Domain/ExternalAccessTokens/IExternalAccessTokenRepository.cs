namespace CatCar.Contexts.Communication.Domain.ExternalAccessTokens;

/// <summary>
/// Persistence port for <see cref="ExternalAccessToken"/> aggregates.
/// Implemented in Infrastructure and registered via RiseOn.AutoInject.
/// </summary>
public interface IExternalAccessTokenRepository
{
    Task<ExternalAccessToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Looks up a token by the SHA-256 hash of its raw value (never by the raw value itself).</summary>
    Task<ExternalAccessToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<ExternalAccessToken?> GetActiveByBudgetIdAsync(Guid budgetId, CancellationToken cancellationToken = default);

    Task AddAsync(ExternalAccessToken token, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
