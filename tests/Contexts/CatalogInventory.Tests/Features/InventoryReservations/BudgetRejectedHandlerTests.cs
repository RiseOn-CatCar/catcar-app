namespace CatCar.Contexts.CatalogInventory.Tests.Features.InventoryReservations;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contexts.CatalogInventory.Features.InventoryReservations.BudgetRejected;
using CatCar.Contexts.CatalogInventory.Infrastructure;
using CatCar.Contracts.ServiceOperations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiseOn.RailResult.Upshot;
using Wolverine.EntityFrameworkCore;
using Xunit;

public class BudgetRejectedHandlerTests
{
    private readonly IInventoryItemRepository _inventoryRepository = Substitute.For<IInventoryItemRepository>();
    private readonly IInventoryReservationRepository _reservationRepository = Substitute.For<IInventoryReservationRepository>();
    private readonly ILogger<BudgetRejectedHandler.Marker> _logger = Substitute.For<ILogger<BudgetRejectedHandler.Marker>>();
    private readonly IDbContextOutbox<CatalogInventoryDbContext> _outbox = Substitute.For<IDbContextOutbox<CatalogInventoryDbContext>>();

    [Fact]
    public async Task Handle_WhenReservationsExist_ShouldReleaseAll()
    {
        var budgetId = Guid.CreateVersion7();
        var workOrderId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();

        var reservation = InventoryReservation.Create(workOrderId, budgetId, itemId, 3, DateTime.UtcNow).Value;
        _reservationRepository.GetActiveReservationsByBudgetIdAsync(budgetId, Arg.Any<CancellationToken>())
            .Returns([reservation]);
        _inventoryRepository.PrepareReleaseAsync(
                Arg.Any<IReadOnlyCollection<InventoryReservation>>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Upshot.Success()));

        var @event = new BudgetRejectedIntegrationEvent(budgetId, workOrderId, Guid.CreateVersion7(), "Muito caro", DateTime.UtcNow, Guid.NewGuid());

        await BudgetRejectedHandler.Handle(@event, _inventoryRepository, _reservationRepository, _outbox, _logger, CancellationToken.None);

        await _inventoryRepository.Received(1).PrepareReleaseAsync(
            Arg.Is<IReadOnlyCollection<InventoryReservation>>(r => r.Count == 1 && r.Single() == reservation),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoReservationsExist_ShouldDoNothing()
    {
        var budgetId = Guid.CreateVersion7();
        _reservationRepository.GetActiveReservationsByBudgetIdAsync(budgetId, Arg.Any<CancellationToken>())
            .Returns([]);

        var @event = new BudgetRejectedIntegrationEvent(budgetId, Guid.CreateVersion7(), Guid.CreateVersion7(), "Muito caro", DateTime.UtcNow, Guid.NewGuid());

        await BudgetRejectedHandler.Handle(@event, _inventoryRepository, _reservationRepository, _outbox, _logger, CancellationToken.None);

        await _inventoryRepository.DidNotReceive().PrepareReleaseAsync(
            Arg.Any<IReadOnlyCollection<InventoryReservation>>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }
}
