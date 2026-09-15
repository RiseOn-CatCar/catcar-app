namespace CatCar.Contexts.CatalogInventory.Tests.Features.InventoryReservations;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contexts.CatalogInventory.Features.InventoryReservations.WorkOrderStatusChanged;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiseOn.RailResult.Upshot;
using Wolverine;
using Xunit;

public class WorkOrderStatusChangedInventoryHandlerTests
{
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly IInventoryItemRepository _inventoryRepository = Substitute.For<IInventoryItemRepository>();
    private readonly IInventoryReservationRepository _reservationRepository = Substitute.For<IInventoryReservationRepository>();
    private readonly ILogger<WorkOrderStatusChangedInventoryHandler.Marker> _logger = Substitute.For<ILogger<WorkOrderStatusChangedInventoryHandler.Marker>>();
    [Fact]
    public async Task Handle_WhenStatusIsCompleted_ShouldConsumeReservationsAndPublishEvent()
    {
        var workOrderId = Guid.CreateVersion7();
        var budgetId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();

        var reservation = InventoryReservation.Create(workOrderId, budgetId, itemId, 2, DateTime.UtcNow).Value;
        _reservationRepository.GetActiveReservationsByWorkOrderIdAsync(workOrderId, Arg.Any<CancellationToken>())
            .Returns([reservation]);
        _inventoryRepository.ConsumeReservationsAsync(
                Arg.Any<IReadOnlyCollection<InventoryReservation>>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Upshot.Success()));

        var @event = new WorkOrderStatusChangedIntegrationEvent(
            workOrderId, Guid.CreateVersion7(), "InExecution", "Completed", DateTime.UtcNow, Guid.NewGuid());

        await WorkOrderStatusChangedInventoryHandler.Handle(@event, _bus, _inventoryRepository, _reservationRepository, _logger, CancellationToken.None);

        await _inventoryRepository.Received(1).ConsumeReservationsAsync(
            Arg.Is<IReadOnlyCollection<InventoryReservation>>(r => r.Count == 1 && r.Single() == reservation),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());

        await _bus.Received(1).PublishAsync(Arg.Is<InventoryConsumedIntegrationEvent>(e =>
            e.WorkOrderId == workOrderId &&
            e.Items.Count == 1 &&
            e.Items[0].InventoryItemId == itemId &&
            e.Items[0].Quantity == 2));
    }

    [Fact]
    public async Task Handle_WhenStatusIsCanceled_ShouldReleaseReservations()
    {
        var workOrderId = Guid.CreateVersion7();
        var budgetId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();

        var reservation = InventoryReservation.Create(workOrderId, budgetId, itemId, 2, DateTime.UtcNow).Value;
        _reservationRepository.GetActiveReservationsByWorkOrderIdAsync(workOrderId, Arg.Any<CancellationToken>())
            .Returns([reservation]);
        _inventoryRepository.ReleaseReservationsAsync(
                Arg.Any<IReadOnlyCollection<InventoryReservation>>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Upshot.Success()));

        var @event = new WorkOrderStatusChangedIntegrationEvent(
            workOrderId, Guid.CreateVersion7(), "InExecution", "Canceled", DateTime.UtcNow, Guid.NewGuid());

        await WorkOrderStatusChangedInventoryHandler.Handle(@event, _bus, _inventoryRepository, _reservationRepository, _logger, CancellationToken.None);

        await _inventoryRepository.Received(1).ReleaseReservationsAsync(
            Arg.Is<IReadOnlyCollection<InventoryReservation>>(r => r.Count == 1 && r.Single() == reservation),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _bus.DidNotReceive().PublishAsync(Arg.Any<InventoryConsumedIntegrationEvent>());
    }

    [Fact]
    public async Task Handle_WhenStatusIsNotCompletedOrCanceled_ShouldDoNothing()
    {
        var workOrderId = Guid.CreateVersion7();
        var @event = new WorkOrderStatusChangedIntegrationEvent(
            workOrderId, Guid.CreateVersion7(), "InDiagnosis", "InExecution", DateTime.UtcNow, Guid.NewGuid());

        await WorkOrderStatusChangedInventoryHandler.Handle(@event, _bus, _inventoryRepository, _reservationRepository, _logger, CancellationToken.None);

        await _reservationRepository.DidNotReceive().GetActiveReservationsByWorkOrderIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _bus.DidNotReceive().PublishAsync(Arg.Any<InventoryConsumedIntegrationEvent>());
    }
}
