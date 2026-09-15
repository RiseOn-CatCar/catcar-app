namespace CatCar.Contexts.ServiceOperations.Domain.WorkOrders;

/// <summary>
/// Status of a WorkOrder (OS) throughout its lifecycle.
/// </summary>
public enum WorkOrderStatus
{
    Received,
    InDiagnosis,
    AwaitingApproval,
    InExecution,
    Completed,
    Delivered
}
