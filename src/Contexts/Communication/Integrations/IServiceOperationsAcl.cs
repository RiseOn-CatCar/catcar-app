namespace CatCar.Contexts.Communication.Integrations;

using RiseOn.RailResult.Upshot;

/// <summary>
/// Anti-corruption layer (ACL) port for querying and commanding the ServiceOperations context. Per the
/// ratified module-interaction sequence (feature 05: "Comunicacao calls the authenticated endpoint
/// RecordBudgetApproval in Atendimento, with the token already validated - ACL on the consumer"),
/// Communication never references ServiceOperations' assembly directly - this port is implemented using
/// Wolverine's in-process message bus against the published-language contracts in
/// <c>CatCar.Contracts.ServiceOperations</c>.
/// </summary>
public interface IServiceOperationsAcl
{
    /// <summary>Reads a Budget snapshot (and its customer's contact details) to address the approval-link e-mail.</summary>
    Task<Upshot<BudgetSnapshot>> GetBudgetSnapshotAsync(Guid budgetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the customer's decision (approval or rejection) against the Budget/WorkOrder aggregates.
    /// Only called after Communication has already validated the external access token - the token itself
    /// is consumed by the caller only once this call succeeds, keeping the whole flow idempotent even under
    /// retries. The status-transition business rule stays in ServiceOperations' domain
    /// (AC: a regra de negócio do status continua no domínio da OS).
    /// </summary>
    Task<Upshot<BudgetDecisionResult>> RecordBudgetDecisionAsync(
        Guid budgetId, bool approved, string? rejectionReason, CancellationToken cancellationToken = default);
}

/// <summary>Local (ACL-translated) result of recording a budget decision.</summary>
public sealed record BudgetDecisionResult(Guid BudgetId, Guid WorkOrderId, string BudgetStatus, string WorkOrderStatus);
