namespace CatCar.Contexts.Communication.Integrations;

using CatCar.Contracts.ServiceOperations;
using RiseOn.AutoInject;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Wolverine-based implementation of <see cref="IServiceOperationsAcl"/>. Dispatches published-language
/// queries/commands in-process (no HTTP, no shared entities) and translates the responses into
/// Communication's own local read models.
/// </summary>
[InjectService(ServiceLifetimeType.Scoped, CollectionName = "Communication")]
public sealed class ServiceOperationsAcl(IMessageBus bus) : IServiceOperationsAcl
{
    public async Task<Upshot<BudgetSnapshot>> GetBudgetSnapshotAsync(Guid budgetId, CancellationToken cancellationToken = default)
    {
        var response = await bus.InvokeAsync<BudgetSnapshotResponse>(
            new GetBudgetSnapshotQuery(budgetId), cancellationToken).ConfigureAwait(false);

        if (!response.Found)
            return Upshot<BudgetSnapshot>.Fail("Orçamento não encontrado.");

        var lines = response.Lines
            .Select(l => new BudgetSnapshotLine(l.Type, l.Description, l.UnitPrice, l.Quantity, l.LineTotal))
            .ToList();

        return Upshot<BudgetSnapshot>.Success(new BudgetSnapshot(
            response.BudgetId,
            response.WorkOrderId,
            response.CustomerId,
            response.CustomerName,
            response.CustomerEmail,
            response.BudgetStatus,
            response.TotalAmount,
            response.IssuedAt,
            lines));
    }

    public async Task<Upshot<BudgetDecisionResult>> RecordBudgetDecisionAsync(
        Guid budgetId, bool approved, string? rejectionReason, CancellationToken cancellationToken = default)
    {
        var response = await bus.InvokeAsync<RecordBudgetDecisionResponse>(
            new RecordBudgetDecisionCommand(budgetId, approved, rejectionReason), cancellationToken).ConfigureAwait(false);

        if (!response.Success)
            return Upshot<BudgetDecisionResult>.Fail(response.Error ?? "Não foi possível registrar a decisão do orçamento.");

        return Upshot<BudgetDecisionResult>.Success(
            new BudgetDecisionResult(response.BudgetId, response.WorkOrderId, response.BudgetStatus, response.WorkOrderStatus));
    }
}
