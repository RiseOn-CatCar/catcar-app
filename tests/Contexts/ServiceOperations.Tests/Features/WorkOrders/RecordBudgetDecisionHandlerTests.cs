namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.RecordBudgetDecision;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contracts.ServiceOperations;
using FluentAssertions;
using NSubstitute;
using Wolverine.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Unit tests covering the pre-outbox business rules of <see cref="RecordBudgetDecisionHandler"/>.
/// The success path (which flows through Wolverine's transactional outbox against a real
/// PostgreSQL-backed DbContext) is covered by the Testcontainers-based integration suite instead.
/// These tests never touch <see cref="IDbContextOutbox{T}.DbContext"/>, so a bare, unconfigured
/// substitute is sufficient (PublishAsync/SaveChangesAndFlushMessagesAsync resolve to completed tasks).
/// </summary>
public class RecordBudgetDecisionHandlerTests
{
    private readonly RecordBudgetDecisionCommandValidator _validator = new();
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly IWorkOrderRepository _workOrderRepository = Substitute.For<IWorkOrderRepository>();
    private readonly IDbContextOutbox<ServiceOperationsDbContext> _outbox = Substitute.For<IDbContextOutbox<ServiceOperationsDbContext>>();

    [Fact]
    public async Task Handle_RejectedWithoutReason_ShouldFailValidationWithoutHittingRepository()
    {
        var command = new RecordBudgetDecisionCommand(Guid.CreateVersion7(), Approved: false, RejectionReason: null, CorrelationId: Guid.NewGuid());

        var response = await RecordBudgetDecisionHandler.Handle(
            command, _validator, _budgetRepository, _workOrderRepository, _outbox, CancellationToken.None);

        response.Success.Should().BeFalse();
        await _budgetRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistingBudget_ShouldFail()
    {
        var command = new RecordBudgetDecisionCommand(Guid.CreateVersion7(), Approved: true, RejectionReason: null, CorrelationId: Guid.NewGuid());
        _budgetRepository.GetByIdAsync(command.BudgetId, Arg.Any<CancellationToken>()).Returns((Budget?)null);

        var response = await RecordBudgetDecisionHandler.Handle(
            command, _validator, _budgetRepository, _workOrderRepository, _outbox, CancellationToken.None);

        response.Success.Should().BeFalse();
        response.Error.Should().Be("Orçamento não encontrado.");
    }

    [Fact]
    public async Task Handle_WithNonExistingWorkOrder_ShouldFail()
    {
        var workOrder = CreateWorkOrderAwaitingApproval();
        var budget = CreateActiveBudget(workOrder.Id);
        var command = new RecordBudgetDecisionCommand(budget.Id, Approved: true, RejectionReason: null, CorrelationId: Guid.NewGuid());
        _budgetRepository.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _workOrderRepository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns((WorkOrder?)null);

        var response = await RecordBudgetDecisionHandler.Handle(
            command, _validator, _budgetRepository, _workOrderRepository, _outbox, CancellationToken.None);

        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenBudgetNotActive_ShouldFailBeforeTouchingWorkOrderTransition()
    {
        var workOrder = CreateWorkOrderAwaitingApproval();
        var budget = CreateActiveBudget(workOrder.Id);
        workOrder.MarkBudgetIssued(budget.Id, DateTime.UtcNow);
        budget.Approve(DateTime.UtcNow); // Budget is no longer Active.

        var command = new RecordBudgetDecisionCommand(budget.Id, Approved: true, RejectionReason: null, CorrelationId: Guid.NewGuid());
        _budgetRepository.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _workOrderRepository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var response = await RecordBudgetDecisionHandler.Handle(
            command, _validator, _budgetRepository, _workOrderRepository, _outbox, CancellationToken.None);

        response.Success.Should().BeFalse();
        workOrder.Status.Should().Be(WorkOrderStatus.AwaitingApproval);
    }

    [Fact]
    public async Task Handle_ApprovedForValidBudgetAndWorkOrder_ShouldSucceedAndPublishThroughOutbox()
    {
        var workOrder = CreateWorkOrderAwaitingApproval();
        var budget = CreateActiveBudget(workOrder.Id);
        workOrder.MarkBudgetIssued(budget.Id, DateTime.UtcNow);

        var command = new RecordBudgetDecisionCommand(budget.Id, Approved: true, RejectionReason: null, CorrelationId: Guid.NewGuid());
        _budgetRepository.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _workOrderRepository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var response = await RecordBudgetDecisionHandler.Handle(
            command, _validator, _budgetRepository, _workOrderRepository, _outbox, CancellationToken.None);

        response.Success.Should().BeTrue();
        response.BudgetStatus.Should().Be(nameof(BudgetStatus.Approved));
        response.WorkOrderStatus.Should().Be(nameof(WorkOrderStatus.AwaitingApproval));
        await _outbox.Received(1).PublishAsync(Arg.Is<BudgetApprovedIntegrationEvent>(e => e.CorrelationId == command.CorrelationId));
        await _outbox.Received(1).SaveChangesAndFlushMessagesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RejectedForValidBudgetAndWorkOrder_ShouldSucceedAndPublishThroughOutbox()
    {
        var workOrder = CreateWorkOrderAwaitingApproval();
        var budget = CreateActiveBudget(workOrder.Id);
        workOrder.MarkBudgetIssued(budget.Id, DateTime.UtcNow);

        var command = new RecordBudgetDecisionCommand(budget.Id, Approved: false, RejectionReason: "Preço acima do esperado", CorrelationId: Guid.NewGuid());
        _budgetRepository.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _workOrderRepository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var response = await RecordBudgetDecisionHandler.Handle(
            command, _validator, _budgetRepository, _workOrderRepository, _outbox, CancellationToken.None);

        response.Success.Should().BeTrue();
        response.BudgetStatus.Should().Be(nameof(BudgetStatus.Rejected));
        response.WorkOrderStatus.Should().Be(nameof(WorkOrderStatus.InDiagnosis));
        await _outbox.Received(1).PublishAsync(Arg.Is<BudgetRejectedIntegrationEvent>(e => e.CorrelationId == command.CorrelationId));
        await _outbox.Received(1).PublishAsync(Arg.Is<WorkOrderStatusChangedIntegrationEvent>(e => e.CorrelationId == command.CorrelationId));
        await _outbox.Received(1).SaveChangesAndFlushMessagesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDecisionWouldBeBackdated_DoesNotApproveBudget()
    {
        var future = DateTime.UtcNow.AddDays(1);
        var workOrder = WorkOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Description", future).Value;
        workOrder.AddRequestedService(Guid.NewGuid(), "Service", 10m, 1);
        var budget = CreateActiveBudget(workOrder.Id);
        workOrder.MarkBudgetIssued(budget.Id, future);
        _budgetRepository.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _workOrderRepository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var response = await RecordBudgetDecisionHandler.Handle(
            new RecordBudgetDecisionCommand(budget.Id, true, null, Guid.NewGuid()), _validator, _budgetRepository, _workOrderRepository, _outbox, CancellationToken.None);

        response.Success.Should().BeFalse();
        budget.Status.Should().Be(BudgetStatus.Active);
        budget.ApprovedAt.Should().BeNull();
        workOrder.ApprovedBudgetId.Should().BeNull();
        await _outbox.DidNotReceive().SaveChangesAndFlushMessagesAsync(Arg.Any<CancellationToken>());
    }

    private static Budget CreateActiveBudget(Guid workOrderId)
    {
        var lines = new List<(BudgetLineType, Guid, string, decimal, int)>
        {
            (BudgetLineType.Service, Guid.CreateVersion7(), "Troca de óleo", 150m, 1),
        };

        return Budget.Issue(workOrderId, lines, DateTime.UtcNow).Value;
    }

    private static WorkOrder CreateWorkOrderAwaitingApproval()
    {
        var workOrder = WorkOrder.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), "Descrição válida", DateTime.UtcNow).Value;
        workOrder.AddRequestedService(Guid.CreateVersion7(), "Troca de óleo", 150m, 1);
        return workOrder;
    }
}
