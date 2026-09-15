namespace CatCar.Contexts.Communication.Tests.Features.StatusNotifications;

using CatCar.Contexts.Communication.Features.StatusNotifications;
using CatCar.Contexts.Communication.Integrations;
using CatCar.Contexts.Communication.Notifications;
using CatCar.Contracts.ServiceOperations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

public class WorkOrderStatusChangedNotificationHandlerTests
{
    private readonly IServiceOperationsAcl _acl = Substitute.For<IServiceOperationsAcl>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ILogger<WorkOrderStatusChangedNotificationHandler.Marker> _logger = Substitute.For<ILogger<WorkOrderStatusChangedNotificationHandler.Marker>>();

    [Theory]
    [InlineData("InDiagnosis")]
    [InlineData("AwaitingApproval")]
    [InlineData("InExecution")]
    [InlineData("Completed")]
    [InlineData("Delivered")]
    public async Task Handle_WithNotifiableStatus_ShouldSendEmail(string status)
    {
        var customerId = Guid.CreateVersion7();
        var workOrderId = Guid.CreateVersion7();
        var @event = new WorkOrderStatusChangedIntegrationEvent(
            workOrderId, customerId, "PreviousStatus", status, DateTime.UtcNow, Guid.NewGuid());

        var customer = new CustomerSnapshot(customerId, "Cliente Teste", "cliente@example.com", "11999999999", true);
        _acl.GetCustomerSnapshotAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(RiseOn.RailResult.Upshot.Upshot<CustomerSnapshot>.Success(customer));

        await WorkOrderStatusChangedNotificationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.ToAddress == "cliente@example.com" && m.ToName == "Cliente Teste"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBudgetRejectionReturnsToDiagnosis_ShouldNotSendEmail()
    {
        var @event = new WorkOrderStatusChangedIntegrationEvent(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "AwaitingApproval",
            "InDiagnosis",
            DateTime.UtcNow,
            Guid.NewGuid());

        await WorkOrderStatusChangedNotificationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _acl.DidNotReceive().GetCustomerSnapshotAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonNotifiableStatus_ShouldNotFetchCustomerOrSendEmail()
    {
        var customerId = Guid.CreateVersion7();
        var workOrderId = Guid.CreateVersion7();
        var @event = new WorkOrderStatusChangedIntegrationEvent(
            workOrderId, customerId, "None", "Received", DateTime.UtcNow, Guid.NewGuid());

        await WorkOrderStatusChangedNotificationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _acl.DidNotReceive().GetCustomerSnapshotAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCustomerHasNoEmail_ShouldNotSendEmail()
    {
        var customerId = Guid.CreateVersion7();
        var workOrderId = Guid.CreateVersion7();
        var @event = new WorkOrderStatusChangedIntegrationEvent(
            workOrderId, customerId, "Received", "InDiagnosis", DateTime.UtcNow, Guid.NewGuid());

        var customer = new CustomerSnapshot(customerId, "Cliente Teste", null, "11999999999", true);
        _acl.GetCustomerSnapshotAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(RiseOn.RailResult.Upshot.Upshot<CustomerSnapshot>.Success(customer));

        await WorkOrderStatusChangedNotificationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
