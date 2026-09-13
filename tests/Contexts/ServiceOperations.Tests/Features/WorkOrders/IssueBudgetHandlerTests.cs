namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.IssueBudget;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using FluentAssertions;
using NSubstitute;
using Wolverine.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Unit tests covering the pre-outbox business rules of <see cref="IssueBudgetHandler"/>.
/// The success path (which flows through Wolverine's transactional outbox against a real
/// PostgreSQL-backed DbContext) is covered by the Testcontainers-based integration suite instead -
/// see Integration/OutboxTests.cs. These tests never reach <see cref="IDbContextOutbox{T}"/> members,
/// so a bare, unconfigured substitute is sufficient.
/// </summary>
public class IssueBudgetHandlerTests
{
    private readonly IssueBudgetCommandValidator _validator = new();
    private readonly IWorkOrderRepository _repository = Substitute.For<IWorkOrderRepository>();
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly IDbContextOutbox<ServiceOperationsDbContext> _outbox = Substitute.For<IDbContextOutbox<ServiceOperationsDbContext>>();

    [Fact]
    public async Task Handle_WithInvalidCommand_ShouldFailValidationWithoutHittingRepository()
    {
        var command = new IssueBudgetCommand(Guid.Empty);

        var result = await IssueBudgetHandler.Handle(command, _validator, _repository, _budgetRepository, _outbox, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistingWorkOrder_ShouldFail()
    {
        var command = new IssueBudgetCommand(Guid.CreateVersion7());
        _repository.GetByIdAsync(command.WorkOrderId, Arg.Any<CancellationToken>()).Returns((WorkOrder?)null);

        var result = await IssueBudgetHandler.Handle(command, _validator, _repository, _budgetRepository, _outbox, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithoutRequestedServicesOrParts_ShouldFailBeforeTouchingOutbox()
    {
        var workOrder = WorkOrder.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), "Descrição válida").Value;
        var command = new IssueBudgetCommand(workOrder.Id);
        _repository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var result = await IssueBudgetHandler.Handle(command, _validator, _repository, _budgetRepository, _outbox, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.Received);
    }
}
