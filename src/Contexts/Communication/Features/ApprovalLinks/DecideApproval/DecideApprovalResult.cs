namespace CatCar.Contexts.Communication.Features.ApprovalLinks.DecideApproval;

/// <summary>Result of successfully recording a customer's approval decision.</summary>
public sealed record DecideApprovalResult(Guid BudgetId, string BudgetStatus, string WorkOrderStatus);
