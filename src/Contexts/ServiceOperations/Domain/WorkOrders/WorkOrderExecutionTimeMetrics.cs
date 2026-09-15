namespace CatCar.Contexts.ServiceOperations.Domain.WorkOrders;

public sealed record WorkOrderExecutionTimeMetrics(
    int TotalCompletedWorkOrders,
    double AverageTotalExecutionTimeHours,
    double AverageDiagnosisTimeHours,
    double AverageExecutionTimeHours)
{
    public static readonly WorkOrderExecutionTimeMetrics Empty = new(0, 0, 0, 0);
}
