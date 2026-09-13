namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.IssueBudget;

/// <summary>
/// Command to issue a budget for a WorkOrder, freezing the currently requested services/parts as a
/// price snapshot and moving the OS to AwaitingApproval (AC: orçamento gerado automaticamente com
/// base nos serviços e peças - feature 04).
/// </summary>
public sealed record IssueBudgetCommand(Guid WorkOrderId);
