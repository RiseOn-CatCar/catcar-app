namespace CatCar.Contexts.ServiceOperations.Features.Telemetry;

using CatCar.Contracts.ServiceOperations;
using Microsoft.Extensions.Logging;

public static partial class WorkOrderStatusTelemetryHandler
{
    public static void Handle(WorkOrderStatusChangedIntegrationEvent message, ILogger<WorkOrderStatusTelemetryLog> logger)
    {
        if (string.IsNullOrEmpty(message.PreviousStatus))
        {
            Opened(logger, message.EventId, message.WorkOrderId, "WorkOrderOpened", message.ChangedAt, message.CorrelationId, "ServiceOperations");
            return;
        }

        var stage = message.PreviousStatus switch
        {
            "InDiagnosis" => "Diagnosis",
            "InExecution" => "Execution",
            "Completed" => "Finalization",
            _ => null
        };
        if (stage is null) return;
        if (message.PreviousStatusStartedAt is null)
        {
            StageCompletedWithoutDuration(logger, message.EventId, message.WorkOrderId, "WorkOrderStageCompleted", stage, message.ChangedAt, message.CorrelationId, "ServiceOperations");
            return;
        }
        var duration = (message.ChangedAt - message.PreviousStatusStartedAt.Value).TotalSeconds;
        if (duration < 0)
        {
            InvalidDuration(logger, message.EventId, message.WorkOrderId, message.CorrelationId);
            return;
        }
        StageCompleted(logger, message.EventId, message.WorkOrderId, "WorkOrderStageCompleted", stage, message.ChangedAt, duration, message.CorrelationId, "ServiceOperations");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Work order opened {EventId} {WorkOrderId} {EventName} {OccurredAtUtc} {CorrelationId} {Component}")]
    private static partial void Opened(ILogger logger, Guid eventId, Guid workOrderId, string eventName, DateTime occurredAtUtc, Guid correlationId, string component);

    [LoggerMessage(Level = LogLevel.Information, Message = "Work order stage completed {EventId} {WorkOrderId} {EventName} {Stage} {OccurredAtUtc} {DurationSeconds} {CorrelationId} {Component}")]
    private static partial void StageCompleted(ILogger logger, Guid eventId, Guid workOrderId, string eventName, string stage, DateTime occurredAtUtc, double durationSeconds, Guid correlationId, string component);

    [LoggerMessage(Level = LogLevel.Information, Message = "Work order stage completed {EventId} {WorkOrderId} {EventName} {Stage} {OccurredAtUtc} {CorrelationId} {Component}")]
    private static partial void StageCompletedWithoutDuration(ILogger logger, Guid eventId, Guid workOrderId, string eventName, string stage, DateTime occurredAtUtc, Guid correlationId, string component);

    [LoggerMessage(Level = LogLevel.Error, Message = "Negative work order stage duration for event {EventId}, work order {WorkOrderId}, correlation {CorrelationId}")]
    private static partial void InvalidDuration(ILogger logger, Guid eventId, Guid workOrderId, Guid correlationId);
}

public sealed class WorkOrderStatusTelemetryLog { }
