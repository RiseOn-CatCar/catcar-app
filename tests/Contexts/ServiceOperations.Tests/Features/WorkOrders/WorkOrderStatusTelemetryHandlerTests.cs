namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using CatCar.Contexts.ServiceOperations.Features.Telemetry;
using CatCar.Contracts.ServiceOperations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

public class WorkOrderStatusTelemetryHandlerTests
{
    [Fact]
    public void Handle_StageExit_LogsStructuredDuration()
    {
        var logger = new CapturingLogger();
        var now = DateTime.UtcNow;
        var message = new WorkOrderStatusChangedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), "InDiagnosis", "AwaitingApproval", now, Guid.NewGuid(), Guid.NewGuid(), now.AddMinutes(-5), Guid.NewGuid());
        WorkOrderStatusTelemetryHandler.Handle(message, logger);
        logger.Level.Should().Be(LogLevel.Information);
        logger.Properties["EventName"].Should().Be("WorkOrderStageCompleted");
        logger.Properties["Stage"].Should().Be("Diagnosis");
        logger.Properties["DurationSeconds"].Should().Be(300d);
        logger.Properties["EventId"].Should().Be(message.EventId);
    }

    [Fact]
    public void Handle_UnknownDuration_OmitsDurationAndNegativeDurationIsError()
    {
        var logger = new CapturingLogger();
        var now = DateTime.UtcNow;
        var message = new WorkOrderStatusChangedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), "Completed", "Delivered", now, Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid());
        WorkOrderStatusTelemetryHandler.Handle(message, logger);
        logger.Properties.Should().NotContainKey("DurationSeconds");
        WorkOrderStatusTelemetryHandler.Handle(message with { PreviousStatusStartedAt = now.AddSeconds(1) }, logger);
        logger.Level.Should().Be(LogLevel.Error);
        logger.Properties.Should().NotContainKey("DurationSeconds");
    }

    private sealed class CapturingLogger : ILogger<WorkOrderStatusTelemetryLog>
    {
        public LogLevel Level { get; private set; }
        public Dictionary<string, object?> Properties { get; private set; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Level = logLevel;
            Properties = ((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }
}
