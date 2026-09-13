namespace CatCar.Contexts.Communication.Tests.Features.ApprovalLinks;

using CatCar.Contexts.Communication.Features.ApprovalLinks.SendApprovalConfirmation;
using CatCar.Contexts.Communication.Integrations;
using CatCar.Contexts.Communication.Notifications;
using CatCar.Contracts.ServiceOperations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiseOn.RailResult.Upshot;
using Xunit;

public class SendApprovalConfirmationHandlerTests
{
    private readonly IServiceOperationsAcl _acl = Substitute.For<IServiceOperationsAcl>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ILogger<SendApprovalConfirmationHandler.Marker> _logger = Substitute.For<ILogger<SendApprovalConfirmationHandler.Marker>>();

    [Fact]
    public async Task Handle_BudgetApproved_WhenSnapshotFails_ShouldNotSendEmail()
    {
        var @event = new BudgetApprovedIntegrationEvent(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 300m, DateTime.UtcNow);
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Fail("Orçamento não encontrado."));

        await SendApprovalConfirmationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BudgetApproved_WhenCustomerHasNoEmail_ShouldNotSendEmail()
    {
        var @event = new BudgetApprovedIntegrationEvent(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 300m, DateTime.UtcNow);
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Success(CreateSnapshot(@event.BudgetId, @event.WorkOrderId, @event.CustomerId, null)));

        await SendApprovalConfirmationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BudgetApproved_WithValidSnapshot_ShouldSendConfirmationEmail()
    {
        var @event = new BudgetApprovedIntegrationEvent(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 300m, DateTime.UtcNow);
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Success(CreateSnapshot(@event.BudgetId, @event.WorkOrderId, @event.CustomerId, "cliente@example.com")));

        await SendApprovalConfirmationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.ToAddress == "cliente@example.com" && m.Subject.Contains("aprovado", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BudgetRejected_WhenSnapshotFails_ShouldNotSendEmail()
    {
        var @event = new BudgetRejectedIntegrationEvent(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Preço alto", DateTime.UtcNow);
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Fail("Orçamento não encontrado."));

        await SendApprovalConfirmationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BudgetRejected_WithValidSnapshot_ShouldSendConfirmationEmail()
    {
        var @event = new BudgetRejectedIntegrationEvent(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Preço alto", DateTime.UtcNow);
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Success(CreateSnapshot(@event.BudgetId, @event.WorkOrderId, @event.CustomerId, "cliente@example.com")));

        await SendApprovalConfirmationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.ToAddress == "cliente@example.com" && m.Subject.Contains("recusado", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());
    }

    private static BudgetSnapshot CreateSnapshot(Guid budgetId, Guid workOrderId, Guid customerId, string? customerEmail) =>
        new(
            budgetId, workOrderId, customerId, "Cliente Teste", customerEmail, "Approved", 300m, DateTime.UtcNow,
            [new CatCar.Contexts.Communication.Integrations.BudgetSnapshotLine("Service", "Troca de óleo", 150m, 1, 150m)]);
}
