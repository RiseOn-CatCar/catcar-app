namespace CatCar.Contexts.Communication.Features.ApprovalLinks.GetApprovalLinkDetails;

/// <summary>
/// Query to resolve the customer-facing view of a Budget from its raw external access token.
/// </summary>
public sealed record GetApprovalLinkDetailsQuery(string RawToken);
