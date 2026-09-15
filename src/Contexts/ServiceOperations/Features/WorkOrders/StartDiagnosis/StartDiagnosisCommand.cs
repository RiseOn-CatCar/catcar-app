namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.StartDiagnosis;

public sealed record StartDiagnosisCommand(Guid WorkOrderId, Guid CorrelationId);
