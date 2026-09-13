namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderById;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="GetWorkOrderByIdQuery"/>.
/// </summary>
public static class GetWorkOrderByIdHandler
{
    public static async Task<Upshot<WorkOrderDetails>> Handle(
        GetWorkOrderByIdQuery query,
        IWorkOrderRepository repository,
        CancellationToken cancellationToken)
    {
        var workOrder = await repository.GetByIdAsync(query.WorkOrderId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null)
            return Upshot<WorkOrderDetails>.Fail("OS não encontrada.");

        var services = workOrder.RequestedServices
            .Select(s => new RequestedServiceLineDetails(s.Id, s.CatalogedServiceId, s.Description, s.UnitPrice, s.Quantity, s.LineTotal))
            .ToList();

        var parts = workOrder.RequestedParts
            .Select(p => new RequestedPartLineDetails(p.Id, p.InventoryItemId, p.Description, p.UnitPrice, p.Quantity, p.LineTotal))
            .ToList();

        return Upshot<WorkOrderDetails>.Success(new WorkOrderDetails(
            workOrder.Id,
            workOrder.CustomerId,
            workOrder.VehicleId,
            workOrder.InitialDescription,
            workOrder.Status.ToString(),
            workOrder.ActiveBudgetId,
            workOrder.OpenedAt,
            services,
            parts));
    }
}
