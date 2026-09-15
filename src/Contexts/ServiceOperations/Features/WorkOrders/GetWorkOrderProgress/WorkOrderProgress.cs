namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderProgress;

public sealed record WorkOrderProgress(Guid WorkOrderId, Guid CustomerId, Guid VehicleId, string Status, IReadOnlyList<WorkOrderProgressStage> Timeline, DateTime OpenedAt, DateTime LastUpdatedAt);
public sealed record WorkOrderProgressStage(string Status, bool Reached, DateTime? ReachedAt);
