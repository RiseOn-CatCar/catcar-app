namespace CatCar.Contexts.Communication.Domain.ExternalAccessTokens;

/// <summary>
/// Lifecycle status of an <see cref="ExternalAccessToken"/>. Active -> Consumed | Expired.
/// </summary>
public enum ExternalAccessTokenStatus
{
    Active,
    Consumed,
    Expired
}
