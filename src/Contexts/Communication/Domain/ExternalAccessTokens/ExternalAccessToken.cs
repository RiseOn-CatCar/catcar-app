namespace CatCar.Contexts.Communication.Domain.ExternalAccessTokens;

using System.Security.Cryptography;
using System.Text;
using CatCar.SharedKernel;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Aggregate root representing the secure, single-use external mechanism by which a customer approves
/// or rejects a budget without an administrative login. Context: Comunicacao com Cliente (Communication).
/// Aliases (forbidden per glossary): "login do cliente", "sessão do cliente".
/// AC: link/token externo seguro, expiração/validação desse acesso (feature 05).
/// </summary>
public sealed class ExternalAccessToken : Entity<Guid>, IAggregateRoot
{
    public Guid BudgetId { get; private set; }

    /// <summary>SHA-256 hash (hex) of the raw token. The raw value is never persisted.</summary>
    public string TokenHash { get; private set; }

    public ExternalAccessTokenStatus Status { get; private set; }

    public DateTime IssuedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? ConsumedAt { get; private set; }

    private ExternalAccessToken(Guid id, Guid budgetId, string tokenHash, DateTime issuedAt, DateTime expiresAt)
        : base(id)
    {
        BudgetId = budgetId;
        TokenHash = tokenHash;
        Status = ExternalAccessTokenStatus.Active;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Issues a new single-use external access token for a budget. Only the SHA-256 hash of the raw token
    /// is persisted on the returned aggregate; the raw value is returned separately so the caller can embed
    /// it in the approval-link e-mail. It is never logged or stored in clear text (AC: segurança do acesso
    /// externo ao orçamento).
    /// </summary>
    public static (ExternalAccessToken Token, string RawToken) Issue(Guid budgetId, TimeSpan ttl)
    {
        var rawToken = GenerateRawToken();
        var issuedAt = DateTime.UtcNow;

        var token = new ExternalAccessToken(Guid.CreateVersion7(), budgetId, Hash(rawToken), issuedAt, issuedAt.Add(ttl));
        return (token, rawToken);
    }

    /// <summary>
    /// Validates a raw token against this aggregate's stored hash without consuming it
    /// (AC: validação do acesso externo). Lazily transitions to Expired once the TTL has elapsed.
    /// </summary>
    public Upshot Validate(string? rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return Upshot.Fail("Token de acesso inválido.");

        if (Status == ExternalAccessTokenStatus.Consumed)
            return Upshot.Fail("Este link de aprovação já foi utilizado.");

        if (Status == ExternalAccessTokenStatus.Expired || DateTime.UtcNow > ExpiresAt)
        {
            Status = ExternalAccessTokenStatus.Expired;
            return Upshot.Fail("Este link de aprovação expirou.");
        }

        if (!FixedTimeEquals(Hash(rawToken), TokenHash))
            return Upshot.Fail("Token de acesso inválido.");

        return Upshot.Success();
    }

    /// <summary>
    /// Marks the token as consumed after a decision (approval or rejection) has been successfully recorded
    /// downstream. Single-use: an already consumed or expired token cannot be consumed again.
    /// </summary>
    public Upshot Consume()
    {
        if (Status == ExternalAccessTokenStatus.Consumed)
            return Upshot.Fail("Este link de aprovação já foi utilizado.");

        if (Status == ExternalAccessTokenStatus.Expired || DateTime.UtcNow > ExpiresAt)
        {
            Status = ExternalAccessTokenStatus.Expired;
            return Upshot.Fail("Este link de aprovação expirou.");
        }

        Status = ExternalAccessTokenStatus.Consumed;
        ConsumedAt = DateTime.UtcNow;
        return Upshot.Success();
    }

    /// <summary>
    /// Explicitly expires the token (background job or lazy path also covered by Validate/Consume).
    /// </summary>
    public Upshot Expire()
    {
        if (Status != ExternalAccessTokenStatus.Active)
            return Upshot.Fail("Só é possível expirar um token ativo.");

        Status = ExternalAccessTokenStatus.Expired;
        return Upshot.Success();
    }

    /// <summary>
    /// Computes the SHA-256 hash (hex) of a raw token value. Exposed so callers (e.g. the approval-link
    /// endpoints) can look up the persisted aggregate by hash before calling <see cref="Validate"/> - the
    /// raw token itself is never persisted or used directly as a lookup key.
    /// </summary>
    public static string ComputeHash(string rawToken) => Hash(rawToken);

    private static string GenerateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static bool FixedTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        return bytesA.Length == bytesB.Length && CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
