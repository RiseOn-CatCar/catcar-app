namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderAverageExecutionTime;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;

public static class GetWorkOrderAverageExecutionTimeHandler
{
    public static async Task<WorkOrderAverageExecutionTime> Handle(GetWorkOrderAverageExecutionTimeQuery query, IWorkOrderRepository repository, CancellationToken cancellationToken)
    {
        var metrics = await repository.GetAverageExecutionTimeAsync(cancellationToken).ConfigureAwait(false);
        return new WorkOrderAverageExecutionTime(
            metrics.TotalCompletedWorkOrders,
            metrics.AverageTotalExecutionTimeHours,
            metrics.AverageDiagnosisTimeHours,
            metrics.AverageExecutionTimeHours);
    }
}
