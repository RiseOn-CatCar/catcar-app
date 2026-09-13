namespace CatCar.Contexts.Communication.Features.ApprovalLinks.DecideApproval;

/// <summary>
/// Command carrying the customer's decision on a budget, submitted through the public approval-link
/// endpoint. <paramref name="Approved"/> is true for "aprovar", false for "recusar".
/// </summary>
public sealed record DecideApprovalCommand(string RawToken, bool Approved, string? Reason);
