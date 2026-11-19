namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.InventoryReservations.InventoryReservationFailed;
using CatCar.Contexts.ServiceOperations.Features.InventoryReservations.InventoryReserved;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Wolverine.EntityFrameworkCore;
using Xunit;

public class InventoryReservationHandlersTests
{
    [Fact]
    public async Task Reserved_TransitionsOnceAndPublishesStatus()
    {
        var (order, budget) = CreateApproved();
        var orders = Substitute.For<IWorkOrderRepository>();
        var budgets = Substitute.For<IBudgetRepository>();
        var outbox = Substitute.For<IDbContextOutbox<ServiceOperationsDbContext>>();
        orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        budgets.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>()).Returns(budget);
        var correlation = Guid.NewGuid();
        var message = new InventoryReservedIntegrationEvent(order.Id, budget.Id, [], DateTime.UtcNow, correlation);

        await InventoryReservedHandler.Handle(message, orders, budgets, outbox, NullLogger<InventoryReservedLog>.Instance, CancellationToken.None);
        await InventoryReservedHandler.Handle(message, orders, budgets, outbox, NullLogger<InventoryReservedLog>.Instance, CancellationToken.None);

        order.Status.Should().Be(WorkOrderStatus.InExecution);
        await outbox.Received(1).PublishAsync(Arg.Is<WorkOrderStatusChangedIntegrationEvent>(e => e.BudgetId == budget.Id && e.CorrelationId == correlation));
        await outbox.Received(1).SaveChangesAndFlushMessagesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failed_ReturnsToDiagnosisAndReplayDoesNotPublish()
    {
        var (order, budget) = CreateApproved();
        var orders = Substitute.For<IWorkOrderRepository>();
        var budgets = Substitute.For<IBudgetRepository>();
        var outbox = Substitute.For<IDbContextOutbox<ServiceOperationsDbContext>>();
        orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        budgets.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>()).Returns(budget);
        var correlation = Guid.NewGuid();
        var message = new InventoryReservationFailedIntegrationEvent(order.Id, budget.Id, "InsufficientStock", DateTime.UtcNow, correlation);

        await InventoryReservationFailedHandler.Handle(message, orders, budgets, outbox, NullLogger<InventoryReservationFailedLog>.Instance, CancellationToken.None);
        await InventoryReservationFailedHandler.Handle(message, orders, budgets, outbox, NullLogger<InventoryReservationFailedLog>.Instance, CancellationToken.None);

        order.Status.Should().Be(WorkOrderStatus.InDiagnosis);
        order.ApprovedBudgetId.Should().Be(budget.Id);
        order.InventoryReservationFailureCode.Should().Be("InsufficientStock");
        await outbox.Received(1).PublishAsync(Arg.Is<WorkOrderStatusChangedIntegrationEvent>(e => e.BudgetId == budget.Id && e.CorrelationId == correlation));
        await outbox.Received(1).SaveChangesAndFlushMessagesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reserved_ForAnOldBudget_DoesNotChangeOrder()
    {
        var (order, budget) = CreateApproved();
        var orders = Substitute.For<IWorkOrderRepository>();
        var budgets = Substitute.For<IBudgetRepository>();
        var outbox = Substitute.For<IDbContextOutbox<ServiceOperationsDbContext>>();
        orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        budgets.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>()).Returns(budget);
        var message = new InventoryReservedIntegrationEvent(order.Id, Guid.NewGuid(), [], DateTime.UtcNow, Guid.NewGuid());

        await InventoryReservedHandler.Handle(message, orders, budgets, outbox, NullLogger<InventoryReservedLog>.Instance, CancellationToken.None);

        order.Status.Should().Be(WorkOrderStatus.AwaitingApproval);
        await outbox.DidNotReceive().SaveChangesAndFlushMessagesAsync(Arg.Any<CancellationToken>());
    }

    private static (WorkOrder, Budget) CreateApproved()
    {
        var now = DateTime.UtcNow;
        var order = WorkOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Description", now).Value;
        var service = Guid.NewGuid();
        order.AddRequestedService(service, "Service", 10m, 1);
        var budget = Budget.Issue(order.Id, [(BudgetLineType.Service, service, "Service", 10m, 1)], now).Value;
        order.MarkBudgetIssued(budget.Id, now);
        budget.Approve(now);
        order.RecordApproval(true, now);
        return (order, budget);
    }
}
