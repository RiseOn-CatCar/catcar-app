namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.ListWorkOrders;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="ListWorkOrdersQuery"/>.
/// </summary>
public static class ListWorkOrdersHandler
{
    public static async Task<Upshot<IReadOnlyList<WorkOrderSummary>>> Handle(
        ListWorkOrdersQuery query,
        IWorkOrderRepository repository,
        CancellationToken cancellationToken)
    {
        WorkOrderStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<WorkOrderStatus>(query.Status, ignoreCase: true, out var parsedStatus))
                return Upshot<IReadOnlyList<WorkOrderSummary>>.Fail("O status informado para filtro é inválido.");

            status = parsedStatus;
        }

        var workOrders = await repository.ListAsync(query.CustomerId, status, query.IncludeClosed, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<WorkOrderSummary> summaries = workOrders
            .Select(w => new WorkOrderSummary(w.Id, w.CustomerId, w.VehicleId, w.Status.ToString(), w.ActiveBudgetId, w.OpenedAt))
            .ToList();

        return Upshot<IReadOnlyList<WorkOrderSummary>>.Success(summaries);
    }
}
