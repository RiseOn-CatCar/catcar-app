namespace CatCar.Contexts.CatalogInventory.Tests.Features.InventoryReservations;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contexts.CatalogInventory.Features.InventoryReservations.BudgetApproved;
using CatCar.Contexts.CatalogInventory.Infrastructure;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Xunit;

public class BudgetApprovedHandlerTests
{
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly IInventoryItemRepository _inventory = Substitute.For<IInventoryItemRepository>();
    private readonly IInventoryReservationRepository _reservations = Substitute.For<IInventoryReservationRepository>();
    private readonly IBudgetReservationOutcomeRepository _outcomes = Substitute.For<IBudgetReservationOutcomeRepository>();
    private readonly IDbContextOutbox<CatalogInventoryDbContext> _outbox = Substitute.For<IDbContextOutbox<CatalogInventoryDbContext>>();
    private readonly ILogger<BudgetApprovedHandler.Marker> _logger = Substitute.For<ILogger<BudgetApprovedHandler.Marker>>();
    private readonly Guid _budget = Guid.NewGuid();
    private readonly Guid _workOrder = Guid.NewGuid();

    private readonly Guid _correlation = Guid.NewGuid();

    private Task Handle() => BudgetApprovedHandler.Handle(new(_budget, _workOrder, Guid.NewGuid(), 10m, DateTime.UtcNow, _correlation),
        _bus, _inventory, _reservations, _outcomes, _outbox, _logger, CancellationToken.None);

    private void Snapshot(params BudgetSnapshotLine[] lines)
    {
        _bus.InvokeAsync<BudgetSnapshotResponse>(Arg.Any<GetBudgetSnapshotQuery>(), Arg.Any<CancellationToken>())
            .Returns(new BudgetSnapshotResponse(true, _budget, _workOrder, Guid.NewGuid(), "Customer", null, "Approved", 10m, DateTime.UtcNow, lines));
    }

    [Fact]
    public async Task Handle_NoParts_PublishesEmptyConfirmationAndOutcome()
    {
        Snapshot(new BudgetSnapshotLine("Service", "Service", 10, 1, 10, Guid.NewGuid()));
        await Handle();
        await _outcomes.Received(1).AddAsync(Arg.Is<BudgetReservationOutcome>(o => o.Outcome == ReservationOutcome.NoParts), Arg.Any<CancellationToken>());
        await _outbox.Received(1).PublishAsync(Arg.Is<InventoryReservedIntegrationEvent>(e => e.Items.Count == 0 && e.CorrelationId == _correlation));
        await _outbox.Received(1).SaveChangesAndFlushMessagesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingOutcome_ReplaysWithoutSnapshotOrStockChange()
    {
        var item = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var reservation = InventoryReservation.Create(_workOrder, _budget, item, 2, timestamp).Value;
        reservation.Release(timestamp);
        _reservations.GetReservationsByBudgetIdAsync(_budget, Arg.Any<CancellationToken>()).Returns([reservation]);
        _outcomes.GetByBudgetIdAsync(_budget, Arg.Any<CancellationToken>())
            .Returns(new BudgetReservationOutcome(_budget, _workOrder, ReservationOutcome.Reserved, null, timestamp, _correlation));
        await Handle();
        await _inventory.DidNotReceive().PrepareReservationAsync(Arg.Any<IReadOnlyCollection<InventoryReservation>>(), Arg.Any<CancellationToken>());
        await _outbox.Received(1).PublishAsync(Arg.Is<InventoryReservedIntegrationEvent>(e => e.ReservedAt == timestamp && e.Items.Count == 1 && e.CorrelationId == _correlation));
    }

    [Fact]
    public async Task Handle_MismatchedHistory_PublishesTerminalFailure()
    {
        var item = Guid.NewGuid();
        Snapshot(new BudgetSnapshotLine("Part", "Part", 5, 2, 10, item));
        _reservations.GetReservationsByBudgetIdAsync(_budget, Arg.Any<CancellationToken>())
            .Returns([InventoryReservation.Create(_workOrder, _budget, item, 1, DateTime.UtcNow).Value]);
        await Handle();
        await _outbox.Received(1).PublishAsync(Arg.Is<InventoryReservationFailedIntegrationEvent>(e => e.FailureCode == "ReservationHistoryMismatch"));
        await _inventory.DidNotReceive().PrepareReservationAsync(Arg.Any<IReadOnlyCollection<InventoryReservation>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InsufficientStock_PublishesFailure()
    {
        Snapshot(new BudgetSnapshotLine("Part", "Part", 5, 2, 10, Guid.NewGuid()));
        _inventory.PrepareReservationAsync(Arg.Any<IReadOnlyCollection<InventoryReservation>>(), Arg.Any<CancellationToken>())
            .Returns(InventoryReservationFailureCode.InsufficientStock);
        await Handle();
        await _outbox.Received(1).PublishAsync(Arg.Is<InventoryReservationFailedIntegrationEvent>(e => e.FailureCode == "InsufficientStock"));
    }

    [Fact]
    public async Task Handle_MatchingHistoricalReservation_DoesNotDeductAgain()
    {
        var item = Guid.NewGuid();
        var reservation = InventoryReservation.Create(_workOrder, _budget, item, 2, DateTime.UtcNow).Value;
        reservation.MarkAsConsumed(DateTime.UtcNow);
        Snapshot(new BudgetSnapshotLine("Part", "Part", 5, 2, 10, item));
        _reservations.GetReservationsByBudgetIdAsync(_budget, Arg.Any<CancellationToken>()).Returns([reservation]);
        await Handle();
        await _outcomes.Received(1).AddAsync(Arg.Is<BudgetReservationOutcome>(o => o.Outcome == ReservationOutcome.Reserved), Arg.Any<CancellationToken>());
        await _inventory.DidNotReceive().PrepareReservationAsync(Arg.Any<IReadOnlyCollection<InventoryReservation>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidLine_PublishesFailureWithoutChangingStock()
    {
        Snapshot(new BudgetSnapshotLine("Part", "Part", 5, 0, 0, Guid.NewGuid()));
        await Handle();
        await _outbox.Received(1).PublishAsync(Arg.Is<InventoryReservationFailedIntegrationEvent>(e => e.FailureCode == "InvalidBudgetLines"));
        await _inventory.DidNotReceive().PrepareReservationAsync(Arg.Any<IReadOnlyCollection<InventoryReservation>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissingSnapshot_PublishesFailure()
    {
        _bus.InvokeAsync<BudgetSnapshotResponse>(Arg.Any<GetBudgetSnapshotQuery>(), Arg.Any<CancellationToken>())
            .Returns(new BudgetSnapshotResponse(false, _budget, _workOrder, Guid.Empty, "", null, "", 0, default, []));
        await Handle();
        await _outbox.Received(1).PublishAsync(Arg.Is<InventoryReservationFailedIntegrationEvent>(e => e.FailureCode == "BudgetSnapshotNotFound"));
    }
}
