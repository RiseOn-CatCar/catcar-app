namespace CatCar.Contexts.ServiceOperations.Domain.WorkOrders;

/// <summary>
/// Status of a WorkOrder (OS) throughout its lifecycle.
/// AC-002 (event-storming): Recebida -> Em diagnóstico -> Aguardando aprovação -> Em execução -> Finalizada -> Entregue.
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
