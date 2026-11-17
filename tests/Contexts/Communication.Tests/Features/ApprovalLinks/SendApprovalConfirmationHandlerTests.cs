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
    private readonly CapturingLogger<SendApprovalConfirmationHandler.Marker> _logger = new();

    [Fact]
    public async Task Handle_BudgetApproved_WhenSnapshotFails_ShouldLogStructuredFailureAndThrow()
    {
        var @event = ApprovedEvent();
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Fail("Orçamento não encontrado."));

        var act = () => SendApprovalConfirmationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        AssertFailureLog(@event.WorkOrderId, @event.BudgetId, @event.CorrelationId, "BudgetSnapshotUnavailable");
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Handle_BudgetApproved_WhenRecipientMissingOrInvalid_ShouldLogStructuredFailureAndThrow(string? email)
    {
        var @event = ApprovedEvent();
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Success(CreateSnapshot(@event.BudgetId, @event.WorkOrderId, @event.CustomerId, email)));

        var act = () => SendApprovalConfirmationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        AssertFailureLog(@event.WorkOrderId, @event.BudgetId, @event.CorrelationId, "InvalidRecipient");
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BudgetApproved_WithValidSnapshot_ShouldSendReceivedConfirmationDependingOnStock()
    {
        var @event = ApprovedEvent();
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Success(CreateSnapshot(@event.BudgetId, @event.WorkOrderId, @event.CustomerId, "cliente@example.com")));

        await SendApprovalConfirmationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.ToAddress == "cliente@example.com"
                && m.Subject.Contains("recebida", StringComparison.OrdinalIgnoreCase)
                && m.Body.Contains("Recebemos a aprovação", StringComparison.Ordinal)
                && m.Body.Contains("depende da disponibilidade de estoque", StringComparison.Ordinal)
                && !m.Body.Contains("já está providenciando", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BudgetApproved_WhenProviderFails_ShouldLogRedactedFailureAndPropagate()
    {
        var @event = ApprovedEvent();
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Success(CreateSnapshot(@event.BudgetId, @event.WorkOrderId, @event.CustomerId, "cliente@example.com")));
        _emailSender.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("failure for cliente@example.com")));

        var act = () => SendApprovalConfirmationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        AssertFailureLog(@event.WorkOrderId, @event.BudgetId, @event.CorrelationId, "EmailProviderFailure");
        _logger.Entries.Should().NotContain(e => e.Message.Contains("cliente@example.com", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_BudgetRejected_WhenSnapshotFails_ShouldLogStructuredFailureAndThrow()
    {
        var @event = RejectedEvent();
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Fail("Orçamento não encontrado."));

        var act = () => SendApprovalConfirmationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        AssertFailureLog(@event.WorkOrderId, @event.BudgetId, @event.CorrelationId, "BudgetSnapshotUnavailable");
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BudgetRejected_WithValidSnapshot_ShouldSendConfirmationEmail()
    {
        var @event = RejectedEvent();
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Success(CreateSnapshot(@event.BudgetId, @event.WorkOrderId, @event.CustomerId, "cliente@example.com")));

        await SendApprovalConfirmationHandler.Handle(@event, _acl, _emailSender, _logger, CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.ToAddress == "cliente@example.com" && m.Subject.Contains("recusado", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());
    }

    private void AssertFailureLog(Guid workOrderId, Guid budgetId, Guid correlationId, string failureCode)
    {
        var entry = _logger.Entries.Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Message.Should().Contain("WorkOrderIntegrationFailed")
            .And.Contain(workOrderId.ToString())
            .And.Contain(budgetId.ToString())
            .And.Contain(correlationId.ToString())
            .And.Contain("Component=Communication")
            .And.Contain("Operation=SendApprovalConfirmation")
            .And.Contain($"FailureCode={failureCode}");
    }

    private static BudgetApprovedIntegrationEvent ApprovedEvent() =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 300m, DateTime.UtcNow, Guid.NewGuid());

    private static BudgetRejectedIntegrationEvent RejectedEvent() =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Preço alto", DateTime.UtcNow, Guid.NewGuid());

    private static BudgetSnapshot CreateSnapshot(Guid budgetId, Guid workOrderId, Guid customerId, string? customerEmail) =>
        new(
            budgetId, workOrderId, customerId, "Cliente Teste", customerEmail, "Approved", 300m, DateTime.UtcNow,
            [new CatCar.Contexts.Communication.Integrations.BudgetSnapshotLine("Service", "Troca de óleo", 150m, 1, 150m)]);
}
