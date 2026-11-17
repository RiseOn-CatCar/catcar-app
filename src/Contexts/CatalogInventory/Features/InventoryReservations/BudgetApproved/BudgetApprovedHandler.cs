namespace CatCar.Contexts.CatalogInventory.Features.InventoryReservations.BudgetApproved;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contexts.CatalogInventory.Infrastructure;
using CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.EntityFrameworkCore;

public static partial class BudgetApprovedHandler
{
    public static async Task Handle(
        BudgetApprovedIntegrationEvent @event, IMessageBus bus, IInventoryItemRepository inventoryRepository,
        IInventoryReservationRepository reservationRepository, IBudgetReservationOutcomeRepository outcomeRepository,
        IDbContextOutbox<CatalogInventoryDbContext> outbox, ILogger<Marker> logger, CancellationToken cancellationToken)
    {
        var outcome = await outcomeRepository.GetByBudgetIdAsync(@event.BudgetId, cancellationToken).ConfigureAwait(false);
        var reservations = await reservationRepository.GetReservationsByBudgetIdAsync(@event.BudgetId, cancellationToken).ConfigureAwait(false);
        if (outcome is not null)
        {
            await PublishOutcome(outcome, reservations, outbox, logger).ConfigureAwait(false);
            await InventoryOutbox.SaveAsync(outbox, cancellationToken).ConfigureAwait(false);
            return;
        }

        var snapshot = await bus.InvokeAsync<BudgetSnapshotResponse>(
            new GetBudgetSnapshotQuery(@event.BudgetId), cancellationToken).ConfigureAwait(false);
        var resolvedAt = DateTime.UtcNow;
        var correlationId = @event.CorrelationId;
        InventoryReservationFailureCode? failure = null;
        var parts = new Dictionary<Guid, int>();
        if (!snapshot.Found || snapshot.BudgetId != @event.BudgetId || snapshot.WorkOrderId != @event.WorkOrderId)
            failure = InventoryReservationFailureCode.BudgetSnapshotNotFound;
        else if (snapshot.Lines.Any(l => l.Quantity <= 0 || l.ReferenceId == Guid.Empty ||
                     !(string.Equals(l.Type, "Part", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(l.Type, "Service", StringComparison.OrdinalIgnoreCase))))
            failure = InventoryReservationFailureCode.InvalidBudgetLines;
        else
        {
            foreach (var group in snapshot.Lines.Where(l => string.Equals(l.Type, "Part", StringComparison.OrdinalIgnoreCase)).GroupBy(l => l.ReferenceId))
            {
                var quantity = group.Sum(l => (long)l.Quantity);
                if (quantity > int.MaxValue)
                {
                    failure = InventoryReservationFailureCode.InvalidBudgetLines;
                    break;
                }
                parts.Add(group.Key, (int)quantity);
            }
        }

        if (failure is null && reservations.Count > 0)
        {
            if (reservations.Any(r => r.WorkOrderId != @event.WorkOrderId) ||
                reservations.Count != parts.Count ||
                reservations.Any(r => !parts.TryGetValue(r.InventoryItemId, out var quantity) || quantity != r.Quantity))
                failure = InventoryReservationFailureCode.ReservationHistoryMismatch;
        }
        else if (failure is null && parts.Count > 0)
        {
            var created = new List<InventoryReservation>(parts.Count);
            foreach (var (itemId, quantity) in parts)
            {
                var result = InventoryReservation.Create(@event.WorkOrderId, @event.BudgetId, itemId, quantity, resolvedAt);
                if (result.IsFailure)
                {
                    failure = InventoryReservationFailureCode.InvalidBudgetLines;
                    break;
                }
                created.Add(result.Value);
            }
            if (failure is null)
            {
                failure = await inventoryRepository.PrepareReservationAsync(created, cancellationToken).ConfigureAwait(false);
                if (failure is null) reservations = created;
            }
        }

        outcome = new BudgetReservationOutcome(@event.BudgetId, @event.WorkOrderId,
            failure.HasValue ? ReservationOutcome.Failed : parts.Count == 0 ? ReservationOutcome.NoParts : ReservationOutcome.Reserved,
            failure, resolvedAt, correlationId);
        await outcomeRepository.AddAsync(outcome, cancellationToken).ConfigureAwait(false);
        await PublishOutcome(outcome, reservations, outbox, logger).ConfigureAwait(false);
        await InventoryOutbox.SaveAsync(outbox, cancellationToken).ConfigureAwait(false);
    }

    private static async Task PublishOutcome(BudgetReservationOutcome outcome, IReadOnlyList<InventoryReservation> reservations,
        IDbContextOutbox<CatalogInventoryDbContext> outbox, ILogger<Marker> logger)
    {
        if (outcome.Outcome == ReservationOutcome.Failed)
        {
            var failureCode = outcome.FailureCode!.Value.ToString();
            LogFailure(logger, "WorkOrderIntegrationFailed", outcome.BudgetId, outcome.WorkOrderId, outcome.BudgetId,
                "CatalogInventory", "ReserveInventory", failureCode, outcome.CorrelationId);
            await outbox.PublishAsync(new InventoryReservationFailedIntegrationEvent(outcome.WorkOrderId, outcome.BudgetId,
                failureCode, outcome.ResolvedAt, outcome.CorrelationId)).ConfigureAwait(false);
        }
        else
        {
            var items = reservations.Select(r => new ReservedInventoryLine(r.InventoryItemId, r.Quantity)).ToList();
            await outbox.PublishAsync(new InventoryReservedIntegrationEvent(outcome.WorkOrderId, outcome.BudgetId, items,
                outcome.ResolvedAt, outcome.CorrelationId)).ConfigureAwait(false);
        }
    }

    public sealed class Marker { public static string Category => "BudgetApproved"; }

    [LoggerMessage(Level = LogLevel.Error, Message = "{EventName} {EventId} {WorkOrderId} {BudgetId} {Component} {Operation} {FailureCode} {CorrelationId}")]
    private static partial void LogFailure(ILogger logger, string eventName, Guid eventId, Guid workOrderId, Guid budgetId,
        string component, string operation, string failureCode, Guid correlationId);
}
