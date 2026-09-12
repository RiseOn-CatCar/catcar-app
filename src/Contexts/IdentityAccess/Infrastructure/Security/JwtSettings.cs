namespace CatCar.Contexts.IdentityAccess.Infrastructure.Security;

/// <summary>
/// Options bound from the "Jwt" configuration section.
/// Production secrets must come from environment variables or a secret manager (e.g. Azure Key Vault),
/// never committed to source control - see docs/security/vulnerability-report.md.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "CatCar";

    public string Audience { get; set; } = "CatCar.AdminApi";

    public int ExpirationMinutes { get; set; } = 60;
}
