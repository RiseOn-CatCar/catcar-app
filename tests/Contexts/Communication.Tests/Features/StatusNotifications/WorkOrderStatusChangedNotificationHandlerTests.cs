namespace CatCar.Contexts.Communication.Tests.Features.StatusNotifications;

using CatCar.Contexts.Communication.Features.StatusNotifications;
using CatCar.Contexts.Communication.Integrations;
using CatCar.Contexts.Communication.Notifications;
using CatCar.Contracts.ServiceOperations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiseOn.RailResult.Upshot;
using Xunit;

public class WorkOrderStatusChangedNotificationHandlerTests
{
    private readonly IServiceOperationsAcl _acl = Substitute.For<IServiceOperationsAcl>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly CapturingLogger<WorkOrderStatusChangedNotificationHandler.Marker> _logger = new();

    [Theory]
    [InlineData("InDiagnosis")]
    [InlineData("AwaitingApproval")]
    [InlineData("InExecution")]
    [InlineData("Completed")]
    [InlineData("Delivered")]
    public async Task Handle_WithNotifiableStatus_ShouldSendEmail(string status)
    {
        var @event = CreateEvent("PreviousStatus", status);
        _acl.GetCustomerSnapshotAsync(@event.CustomerId, Arg.Any<CancellationToken>())
            .Returns(Upshot<CustomerSnapshot>.Success(CreateCustomer(@event, "cliente@example.com")));

        await WorkOrderStatusChangedNotificationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.ToAddress == "cliente@example.com" && m.ToName == "Cliente Teste"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBudgetRejectionReturnsToDiagnosis_ShouldNotSendEmail()
    {
        var @event = CreateEvent("AwaitingApproval", "InDiagnosis");

        await WorkOrderStatusChangedNotificationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _acl.DidNotReceive().GetCustomerSnapshotAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonNotifiableStatus_ShouldNotFetchCustomerOrSendEmail()
    {
        var @event = CreateEvent("None", "Received");

        await WorkOrderStatusChangedNotificationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _acl.DidNotReceive().GetCustomerSnapshotAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCustomerSnapshotUnavailable_ShouldLogStructuredFailureAndThrow()
    {
        var @event = CreateEvent("Received", "InDiagnosis");
        _acl.GetCustomerSnapshotAsync(@event.CustomerId, Arg.Any<CancellationToken>())
            .Returns(Upshot<CustomerSnapshot>.Fail("Cliente não encontrado."));

        var act = () => WorkOrderStatusChangedNotificationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        AssertFailureLog(@event, "CustomerSnapshotUnavailable");
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Handle_WhenRecipientMissingOrInvalid_ShouldLogStructuredFailureAndThrow(string? email)
    {
        var @event = CreateEvent("Received", "InDiagnosis");
        _acl.GetCustomerSnapshotAsync(@event.CustomerId, Arg.Any<CancellationToken>())
            .Returns(Upshot<CustomerSnapshot>.Success(CreateCustomer(@event, email)));

        var act = () => WorkOrderStatusChangedNotificationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        AssertFailureLog(@event, "InvalidRecipient");
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProviderFails_ShouldLogRedactedFailureAndPropagate()
    {
        var @event = CreateEvent("Received", "InDiagnosis");
        _acl.GetCustomerSnapshotAsync(@event.CustomerId, Arg.Any<CancellationToken>())
            .Returns(Upshot<CustomerSnapshot>.Success(CreateCustomer(@event, "cliente@example.com")));
        _emailSender.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("failure for cliente@example.com")));

        var act = () => WorkOrderStatusChangedNotificationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        AssertFailureLog(@event, "EmailProviderFailure");
        _logger.Entries.Should().NotContain(e => e.Message.Contains("cliente@example.com", StringComparison.Ordinal));
    }

    private void AssertFailureLog(WorkOrderStatusChangedIntegrationEvent @event, string failureCode)
    {
        var entry = _logger.Entries.Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Message.Should().Contain("WorkOrderIntegrationFailed")
            .And.Contain(@event.WorkOrderId.ToString())
            .And.Contain(@event.CorrelationId.ToString())
            .And.Contain(@event.EventId.ToString())
            .And.Contain("Component=Communication")
            .And.Contain("Operation=SendStatusNotification")
            .And.Contain($"FailureCode={failureCode}");
    }

    private static WorkOrderStatusChangedIntegrationEvent CreateEvent(string previousStatus, string newStatus) =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), previousStatus, newStatus, DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid());

    private static CustomerSnapshot CreateCustomer(WorkOrderStatusChangedIntegrationEvent @event, string? email) =>
        new(@event.CustomerId, "Cliente Teste", email, "11999999999", true);
}
