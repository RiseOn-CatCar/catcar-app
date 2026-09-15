namespace CatCar.Contexts.CatalogInventory.Tests.Features.InventoryReservations;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contexts.CatalogInventory.Features.InventoryReservations.BudgetApproved;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiseOn.RailResult.Upshot;
using Wolverine;
using Xunit;

public class BudgetApprovedHandlerTests
{
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly IInventoryItemRepository _inventoryRepository = Substitute.For<IInventoryItemRepository>();
    private readonly IInventoryReservationRepository _reservationRepository = Substitute.For<IInventoryReservationRepository>();
    private readonly ILogger<BudgetApprovedHandler.Marker> _logger = Substitute.For<ILogger<BudgetApprovedHandler.Marker>>();

    [Fact]
    public async Task Handle_WithValidPartLines_ShouldReserveInventoryAndPublishEvent()
    {
        var budgetId = Guid.CreateVersion7();
        var workOrderId = Guid.CreateVersion7();
        var customerId = Guid.CreateVersion7();
        var partId = Guid.CreateVersion7();

        _reservationRepository.GetActiveReservationsByBudgetIdAsync(budgetId, Arg.Any<CancellationToken>())
            .Returns([]);
        _inventoryRepository.ReserveAsync(
                Arg.Any<IReadOnlyCollection<InventoryReservation>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Upshot.Success()));

        var budgetSnapshot = new BudgetSnapshotResponse(
            Found: true,
            BudgetId: budgetId,
            WorkOrderId: workOrderId,
            CustomerId: customerId,
            CustomerName: "João Silva",
            CustomerEmail: "joao@example.com",
            BudgetStatus: "Issued",
            TotalAmount: 80m,
            IssuedAt: DateTime.UtcNow,
            Lines: [new BudgetSnapshotLine("Part", "Filtro de óleo", 40m, 2, 80m, partId)]);

        _bus.InvokeAsync<BudgetSnapshotResponse>(Arg.Any<GetBudgetSnapshotQuery>(), Arg.Any<CancellationToken>())
            .Returns(budgetSnapshot);

        var @event = new BudgetApprovedIntegrationEvent(budgetId, workOrderId, customerId, 80m, DateTime.UtcNow);

        await BudgetApprovedHandler.Handle(@event, _bus, _inventoryRepository, _reservationRepository, _logger, CancellationToken.None);

        await _inventoryRepository.Received(1).ReserveAsync(
            Arg.Is<IReadOnlyCollection<InventoryReservation>>(r =>
                r.Count == 1 &&
                r.Single().InventoryItemId == partId &&
                r.Single().Quantity == 2),
            Arg.Any<CancellationToken>());

        await _bus.Received(1).PublishAsync(Arg.Is<InventoryReservedIntegrationEvent>(e =>
            e.WorkOrderId == workOrderId &&
            e.BudgetId == budgetId &&
            e.Items.Count == 1 &&
            e.Items[0].InventoryItemId == partId &&
            e.Items[0].Quantity == 2));
    }

    [Fact]
    public async Task Handle_WhenBudgetNotFound_ShouldNotReserveOrPublish()
    {
        var budgetId = Guid.CreateVersion7();
        var @event = new BudgetApprovedIntegrationEvent(budgetId, Guid.CreateVersion7(), Guid.CreateVersion7(), 80m, DateTime.UtcNow);

        _bus.InvokeAsync<BudgetSnapshotResponse>(Arg.Any<GetBudgetSnapshotQuery>(), Arg.Any<CancellationToken>())
            .Returns(new BudgetSnapshotResponse(false, budgetId, Guid.Empty, Guid.Empty, string.Empty, null, string.Empty, 0m, default, []));

        await BudgetApprovedHandler.Handle(@event, _bus, _inventoryRepository, _reservationRepository, _logger, CancellationToken.None);

        await _inventoryRepository.DidNotReceive().ReserveAsync(
            Arg.Any<IReadOnlyCollection<InventoryReservation>>(),
            Arg.Any<CancellationToken>());
        await _bus.DidNotReceive().PublishAsync(Arg.Any<InventoryReservedIntegrationEvent>());
    }

    [Fact]
    public async Task Handle_WhenBudgetHasNoPartLines_ShouldNotReserveOrPublish()
    {
        var budgetId = Guid.CreateVersion7();
        var workOrderId = Guid.CreateVersion7();
        var serviceId = Guid.CreateVersion7();

        var budgetSnapshot = new BudgetSnapshotResponse(
            Found: true,
            BudgetId: budgetId,
            WorkOrderId: workOrderId,
            CustomerId: Guid.CreateVersion7(),
            CustomerName: "João Silva",
            CustomerEmail: "joao@example.com",
            BudgetStatus: "Issued",
            TotalAmount: 100m,
            IssuedAt: DateTime.UtcNow,
            Lines: [new BudgetSnapshotLine("Service", "Troca de óleo", 100m, 1, 100m, serviceId)]);

        _bus.InvokeAsync<BudgetSnapshotResponse>(Arg.Any<GetBudgetSnapshotQuery>(), Arg.Any<CancellationToken>())
            .Returns(budgetSnapshot);

        var @event = new BudgetApprovedIntegrationEvent(budgetId, workOrderId, Guid.CreateVersion7(), 100m, DateTime.UtcNow);

        await BudgetApprovedHandler.Handle(@event, _bus, _inventoryRepository, _reservationRepository, _logger, CancellationToken.None);

        await _inventoryRepository.DidNotReceive().ReserveAsync(
            Arg.Any<IReadOnlyCollection<InventoryReservation>>(),
            Arg.Any<CancellationToken>());
        await _bus.DidNotReceive().PublishAsync(Arg.Any<InventoryReservedIntegrationEvent>());
    }
}
