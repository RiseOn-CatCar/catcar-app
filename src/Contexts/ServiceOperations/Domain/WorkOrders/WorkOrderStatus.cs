namespace CatCar.Contexts.ServiceOperations.Domain.WorkOrders;

/// <summary>
/// Status of a WorkOrder (OS) throughout its lifecycle.
/// AC-002 (event-storming): Recebida -> Em diagnóstico -> Aguardando aprovação -> Em execução -> Finalizada -> Entregue.
/// AC (feature 05): Aguardando aprovação -> Rejeitada quando o cliente recusa o orçamento vigente
/// (terminal para esse orçamento - AC-018/019 aprovacao-orcamento-e-comunicacao-cliente).
/// </summary>
public enum WorkOrderStatus
{
    Received,
    InDiagnosis,
    AwaitingApproval,
    InExecution,
    Rejected,
    Completed,
    Delivered
}
