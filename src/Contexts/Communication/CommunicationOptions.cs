namespace CatCar.Contexts.Communication;

/// <summary>
/// Configuration for the Communication Bounded Context (feature 05), bound from the "Communication"
/// configuration section.
/// </summary>
public sealed class CommunicationOptions
{
    public const string SectionName = "Communication";

    /// <summary>
    /// Public base URL used to build the customer-facing approval link
    /// (e.g. "https://catcar.example.com"). No real domain exists for this fictitious workshop, so a
    /// placeholder value is used - see README for details.
    /// </summary>
    public string ExternalBaseUrl { get; set; } = "https://catcar.example.com";

    /// <summary>Time-to-live, in days, for an issued external approval-link token.</summary>
    public int ApprovalLinkTtlDays { get; set; } = 7;
}
