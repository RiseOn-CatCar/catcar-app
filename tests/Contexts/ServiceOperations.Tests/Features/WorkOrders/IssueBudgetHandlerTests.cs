namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using CatCar.Contexts.ServiceOperations.Domain.Customers;
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
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly IDbContextOutbox<ServiceOperationsDbContext> _outbox = Substitute.For<IDbContextOutbox<ServiceOperationsDbContext>>();

    [Fact]
    public async Task Handle_WithInvalidCommand_ShouldFailValidationWithoutHittingRepository()
    {
        var command = new IssueBudgetCommand(Guid.Empty, Guid.NewGuid());

        var result = await IssueBudgetHandler.Handle(command, _validator, _repository, _customerRepository, _budgetRepository, _outbox, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistingWorkOrder_ShouldFail()
    {
        var command = new IssueBudgetCommand(Guid.CreateVersion7(), Guid.NewGuid());
        _repository.GetByIdAsync(command.WorkOrderId, Arg.Any<CancellationToken>()).Returns((WorkOrder?)null);

        var result = await IssueBudgetHandler.Handle(command, _validator, _repository, _customerRepository, _budgetRepository, _outbox, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithoutRequestedServicesOrParts_ShouldFailBeforeTouchingOutbox()
    {
        var customer = Customer.Register("52998224725", "Customer", "123456", "customer@example.com").Value;
        var workOrder = WorkOrder.Open(customer.Id, Guid.CreateVersion7(), "Descrição válida", DateTime.UtcNow).Value;
        var command = new IssueBudgetCommand(workOrder.Id, Guid.NewGuid());
        _repository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await IssueBudgetHandler.Handle(command, _validator, _repository, _customerRepository, _budgetRepository, _outbox, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.Received);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public async Task Handle_CustomerWithoutValidEmail_RejectsWithoutWrites(string? email)
    {
        var customer = Customer.Register("52998224725", "Customer", "123456", email).Value;
        var workOrder = WorkOrder.Open(customer.Id, Guid.NewGuid(), "Description", DateTime.UtcNow).Value;
        _repository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await IssueBudgetHandler.Handle(new IssueBudgetCommand(workOrder.Id, Guid.NewGuid()), _validator,
            _repository, _customerRepository, _budgetRepository, _outbox, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("A valid customer email is required to issue a budget.");
        workOrder.Status.Should().Be(WorkOrderStatus.Received);
        await _budgetRepository.DidNotReceive().AddAsync(Arg.Any<Budget>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().SaveChangesAndFlushMessagesAsync(Arg.Any<CancellationToken>());
    }
}
