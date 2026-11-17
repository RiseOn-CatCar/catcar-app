namespace CatCar.Contexts.CatalogInventory.Tests.Features.InventoryReservations;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contexts.CatalogInventory.Features.InventoryReservations.WorkOrderStatusChanged;
using CatCar.Contexts.CatalogInventory.Infrastructure;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using NSubstitute;
using RiseOn.RailResult.Upshot;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Xunit;

public class WorkOrderStatusChangedInventoryHandlerTests
{
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly IInventoryItemRepository _inventory = Substitute.For<IInventoryItemRepository>();
    private readonly IInventoryReservationRepository _reservations = Substitute.For<IInventoryReservationRepository>();
    private readonly IBudgetReservationOutcomeRepository _outcomes = Substitute.For<IBudgetReservationOutcomeRepository>();
    private readonly IDbContextOutbox<CatalogInventoryDbContext> _outbox = Substitute.For<IDbContextOutbox<CatalogInventoryDbContext>>();

    [Fact]
    public async Task Handle_Completed_ConsumesOnlyApprovedBudgetAndPublishes()
    {
        var workOrder = Guid.NewGuid();
        var budget = Guid.NewGuid();
        var item = Guid.NewGuid();
        var reservation = InventoryReservation.Create(workOrder, budget, item, 2, DateTime.UtcNow).Value;
        _outcomes.GetByBudgetIdAsync(budget, Arg.Any<CancellationToken>())
            .Returns(new BudgetReservationOutcome(budget, workOrder, ReservationOutcome.Reserved, null, DateTime.UtcNow, Guid.NewGuid()));
        _reservations.GetReservationsByBudgetIdAsync(budget, Arg.Any<CancellationToken>()).Returns([reservation]);
        _bus.InvokeAsync<BudgetSnapshotResponse>(Arg.Any<GetBudgetSnapshotQuery>(), Arg.Any<CancellationToken>())
            .Returns(new BudgetSnapshotResponse(true, budget, workOrder, Guid.NewGuid(), "Customer", null, "Approved", 10, DateTime.UtcNow,
                [new BudgetSnapshotLine("Part", "Part", 5, 2, 10, item)]));
        _inventory.PrepareConsumptionAsync(Arg.Any<IReadOnlyCollection<InventoryReservation>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Upshot.Success());
        var message = new WorkOrderStatusChangedIntegrationEvent(workOrder, Guid.NewGuid(), "InExecution", "Completed",
            DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), null, budget);

        await WorkOrderStatusChangedInventoryHandler.Handle(message, _bus, _inventory, _reservations, _outcomes, _outbox, CancellationToken.None);
        await _inventory.Received(1).PrepareConsumptionAsync(Arg.Is<IReadOnlyCollection<InventoryReservation>>(r => r.Count == 1 && r.Single() == reservation),
            Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _outbox.Received(1).PublishAsync(Arg.Is<InventoryConsumedIntegrationEvent>(e => e.Items.Count == 1));
        await _outbox.Received(1).SaveChangesAndFlushMessagesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CompletedWithoutOutcome_Throws()
    {
        var message = new WorkOrderStatusChangedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), "InExecution", "Completed",
            DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid());
        await Assert.ThrowsAsync<InvalidOperationException>(() => WorkOrderStatusChangedInventoryHandler.Handle(
            message, _bus, _inventory, _reservations, _outcomes, _outbox, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CompletedWithoutParts_DoesNotConsume()
    {
        var workOrder = Guid.NewGuid();
        var budget = Guid.NewGuid();
        _outcomes.GetByBudgetIdAsync(budget, Arg.Any<CancellationToken>())
            .Returns(new BudgetReservationOutcome(budget, workOrder, ReservationOutcome.NoParts, null, DateTime.UtcNow, Guid.NewGuid()));
        var message = new WorkOrderStatusChangedIntegrationEvent(workOrder, Guid.NewGuid(), "InExecution", "Completed",
            DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), null, budget);
        await WorkOrderStatusChangedInventoryHandler.Handle(message, _bus, _inventory, _reservations, _outcomes, _outbox, CancellationToken.None);
        await _inventory.DidNotReceive().PrepareConsumptionAsync(Arg.Any<IReadOnlyCollection<InventoryReservation>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CompletedWithReleasedReservation_Throws()
    {
        var workOrder = Guid.NewGuid();
        var budget = Guid.NewGuid();
        var item = Guid.NewGuid();
        var reservation = InventoryReservation.Create(workOrder, budget, item, 2, DateTime.UtcNow).Value;
        reservation.Release(DateTime.UtcNow);
        _outcomes.GetByBudgetIdAsync(budget, Arg.Any<CancellationToken>())
            .Returns(new BudgetReservationOutcome(budget, workOrder, ReservationOutcome.Reserved, null, DateTime.UtcNow, Guid.NewGuid()));
        _reservations.GetReservationsByBudgetIdAsync(budget, Arg.Any<CancellationToken>()).Returns([reservation]);
        _bus.InvokeAsync<BudgetSnapshotResponse>(Arg.Any<GetBudgetSnapshotQuery>(), Arg.Any<CancellationToken>())
            .Returns(new BudgetSnapshotResponse(true, budget, workOrder, Guid.NewGuid(), "Customer", null, "Approved", 10, DateTime.UtcNow,
                [new BudgetSnapshotLine("Part", "Part", 5, 2, 10, item)]));
        var message = new WorkOrderStatusChangedIntegrationEvent(workOrder, Guid.NewGuid(), "InExecution", "Completed",
            DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), null, budget);
        await Assert.ThrowsAsync<InvalidOperationException>(() => WorkOrderStatusChangedInventoryHandler.Handle(
            message, _bus, _inventory, _reservations, _outcomes, _outbox, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CompletedWithPartialReservationHistory_Throws()
    {
        var workOrder = Guid.NewGuid();
        var budget = Guid.NewGuid();
        var item = Guid.NewGuid();
        _outcomes.GetByBudgetIdAsync(budget, Arg.Any<CancellationToken>())
            .Returns(new BudgetReservationOutcome(budget, workOrder, ReservationOutcome.Reserved, null, DateTime.UtcNow, Guid.NewGuid()));
        _reservations.GetReservationsByBudgetIdAsync(budget, Arg.Any<CancellationToken>())
            .Returns([InventoryReservation.Create(workOrder, budget, item, 1, DateTime.UtcNow).Value]);
        _bus.InvokeAsync<BudgetSnapshotResponse>(Arg.Any<GetBudgetSnapshotQuery>(), Arg.Any<CancellationToken>())
            .Returns(new BudgetSnapshotResponse(true, budget, workOrder, Guid.NewGuid(), "Customer", null, "Approved", 10, DateTime.UtcNow,
                [new BudgetSnapshotLine("Part", "Part", 5, 2, 10, item)]));
        var message = new WorkOrderStatusChangedIntegrationEvent(workOrder, Guid.NewGuid(), "InExecution", "Completed",
            DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), null, budget);
        await Assert.ThrowsAsync<InvalidOperationException>(() => WorkOrderStatusChangedInventoryHandler.Handle(
            message, _bus, _inventory, _reservations, _outcomes, _outbox, CancellationToken.None));
    }
}
