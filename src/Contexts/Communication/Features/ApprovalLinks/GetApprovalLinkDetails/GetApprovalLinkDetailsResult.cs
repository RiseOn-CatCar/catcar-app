namespace CatCar.Contexts.Communication.Features.ApprovalLinks.GetApprovalLinkDetails;

/// <summary>
/// Customer-facing view of a Budget, returned by the public approval-link endpoint.
/// </summary>
public sealed record GetApprovalLinkDetailsResult(
    Guid BudgetId,
    string CustomerName,
    string BudgetStatus,
    decimal TotalAmount,
    DateTime IssuedAt,
    DateTime ExpiresAt,
    IReadOnlyList<GetApprovalLinkDetailsLine> Lines);

/// <summary>A single frozen line of the budget, as shown to the customer.</summary>
public sealed record GetApprovalLinkDetailsLine(string Type, string Description, decimal UnitPrice, int Quantity, decimal LineTotal);
