namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderProgress;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using RiseOn.RailResult.Upshot;

public static class GetWorkOrderProgressHandler
{
    public static async Task<Upshot<WorkOrderProgress>> Handle(GetWorkOrderProgressQuery query, IWorkOrderRepository repository, CancellationToken cancellationToken)
    {
        var workOrder = await repository.GetByIdAsync(query.WorkOrderId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null) return Upshot<WorkOrderProgress>.Fail("OS não encontrada.");

        var timeline = new[]
        {
            new WorkOrderProgressStage(nameof(WorkOrderStatus.Received), true, workOrder.OpenedAt),
            new WorkOrderProgressStage(nameof(WorkOrderStatus.InDiagnosis), workOrder.DiagnosisStartedAt.HasValue, workOrder.DiagnosisStartedAt),
            new WorkOrderProgressStage(nameof(WorkOrderStatus.AwaitingApproval), workOrder.Status is WorkOrderStatus.AwaitingApproval or WorkOrderStatus.InExecution or WorkOrderStatus.Completed or WorkOrderStatus.Delivered, null),
            new WorkOrderProgressStage(nameof(WorkOrderStatus.InExecution), workOrder.BudgetApprovedAt.HasValue, workOrder.BudgetApprovedAt),
            new WorkOrderProgressStage(nameof(WorkOrderStatus.Completed), workOrder.CompletedAt.HasValue, workOrder.CompletedAt),
            new WorkOrderProgressStage(nameof(WorkOrderStatus.Delivered), workOrder.DeliveredAt.HasValue, workOrder.DeliveredAt)
        };
        return Upshot<WorkOrderProgress>.Success(new WorkOrderProgress(workOrder.Id, workOrder.CustomerId, workOrder.VehicleId, workOrder.Status.ToString(), timeline, workOrder.OpenedAt, workOrder.LastUpdatedAt));
    }
}
