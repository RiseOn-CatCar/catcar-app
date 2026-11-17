namespace CatCar.Contexts.CatalogInventory.Features.InventoryReservations.WorkOrderStatusChanged;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contexts.CatalogInventory.Infrastructure;
using CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using Wolverine;
using Wolverine.EntityFrameworkCore;

public static class WorkOrderStatusChangedInventoryHandler
{
    public static async Task Handle(WorkOrderStatusChangedIntegrationEvent @event, IMessageBus bus,
        IInventoryItemRepository inventoryRepository, IInventoryReservationRepository reservationRepository,
        IBudgetReservationOutcomeRepository outcomeRepository, IDbContextOutbox<CatalogInventoryDbContext> outbox,
        CancellationToken cancellationToken)
    {
        var completed = string.Equals(@event.NewStatus, "Completed", StringComparison.OrdinalIgnoreCase);
        var canceled = string.Equals(@event.NewStatus, "Canceled", StringComparison.OrdinalIgnoreCase);
        if (!completed && !canceled) return;
        if (@event.BudgetId is not Guid budgetId)
            throw new InvalidOperationException("A completed or canceled work order must identify its approved budget.");

        var outcome = await outcomeRepository.GetByBudgetIdAsync(budgetId, cancellationToken).ConfigureAwait(false);
        var reservations = await reservationRepository.GetReservationsByBudgetIdAsync(budgetId, cancellationToken).ConfigureAwait(false);
        if (outcome is null || outcome.WorkOrderId != @event.WorkOrderId || outcome.Outcome == ReservationOutcome.Failed)
            throw new InvalidOperationException("A successful inventory outcome is required.");
        if (outcome.Outcome == ReservationOutcome.NoParts)
        {
            if (reservations.Count != 0) throw new InvalidOperationException("A no-parts outcome has reservations.");
            return;
        }

        var snapshot = await bus.InvokeAsync<BudgetSnapshotResponse>(new GetBudgetSnapshotQuery(budgetId), cancellationToken).ConfigureAwait(false);
        if (!snapshot.Found || snapshot.WorkOrderId != @event.WorkOrderId)
            throw new InvalidOperationException("The approved budget snapshot is unavailable.");
        var parts = snapshot.Lines.Where(l => string.Equals(l.Type, "Part", StringComparison.OrdinalIgnoreCase))
            .GroupBy(l => l.ReferenceId).ToDictionary(g => g.Key, g => g.Sum(l => (long)l.Quantity));
        if (parts.Count == 0 || parts.Count != reservations.Count ||
            reservations.Any(r => r.WorkOrderId != @event.WorkOrderId || !parts.TryGetValue(r.InventoryItemId, out var quantity) || quantity != r.Quantity))
            throw new InvalidOperationException("Inventory reservation history does not match the approved budget.");

        var terminalStatus = completed ? InventoryReservationStatus.Consumed : InventoryReservationStatus.Released;
        if (reservations.All(r => r.Status == terminalStatus)) return;
        if (reservations.Any(r => r.Status != InventoryReservationStatus.Reserved))
            throw new InvalidOperationException("Inventory reservation states are inconsistent.");

        var now = DateTime.UtcNow;
        var result = completed
            ? await inventoryRepository.PrepareConsumptionAsync(reservations, now, cancellationToken).ConfigureAwait(false)
            : await inventoryRepository.PrepareReleaseAsync(reservations, now, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) throw new InvalidOperationException(result.Error.Message);
        if (completed)
            await outbox.PublishAsync(new InventoryConsumedIntegrationEvent(@event.WorkOrderId,
                reservations.Select(r => new ConsumedInventoryLine(r.InventoryItemId, r.Quantity)).ToList(), now)).ConfigureAwait(false);
        await InventoryOutbox.SaveAsync(outbox, cancellationToken).ConfigureAwait(false);
    }
}
