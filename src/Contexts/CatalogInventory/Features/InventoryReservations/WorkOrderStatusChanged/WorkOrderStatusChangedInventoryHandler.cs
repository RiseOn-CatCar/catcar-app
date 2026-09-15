namespace CatCar.Contexts.CatalogInventory.Features.InventoryReservations.WorkOrderStatusChanged;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Wolverine handler consuming <see cref="WorkOrderStatusChangedIntegrationEvent"/>.
/// When NewStatus == "Completed", consumes reserved inventory without applying a second stock movement,
/// updates reservation status to Consumed, and publishes <see cref="InventoryConsumedIntegrationEvent"/>.
/// When NewStatus == "Canceled", releases reserved inventory back to stock.
/// </summary>
public static partial class WorkOrderStatusChangedInventoryHandler
{
    public static async Task Handle(
        WorkOrderStatusChangedIntegrationEvent @event,
        IMessageBus bus,
        IInventoryItemRepository inventoryRepository,
        IInventoryReservationRepository reservationRepository,
        ILogger<Marker> logger,
        CancellationToken cancellationToken)
    {
        if (string.Equals(@event.NewStatus, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            await HandleCompletedWorkOrder(@event, bus, inventoryRepository, reservationRepository, logger, cancellationToken).ConfigureAwait(false);
        }
        else if (string.Equals(@event.NewStatus, "Canceled", StringComparison.OrdinalIgnoreCase))
        {
            await HandleCanceledWorkOrder(@event, inventoryRepository, reservationRepository, logger, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task HandleCompletedWorkOrder(
        WorkOrderStatusChangedIntegrationEvent @event,
        IMessageBus bus,
        IInventoryItemRepository inventoryRepository,
        IInventoryReservationRepository reservationRepository,
        ILogger<Marker> logger,
        CancellationToken cancellationToken)
    {
        var reservations = await reservationRepository.GetActiveReservationsByWorkOrderIdAsync(
            @event.WorkOrderId, cancellationToken).ConfigureAwait(false);

        if (reservations.Count == 0)
        {
            LogNoActiveReservations(logger, @event.WorkOrderId);
            return;
        }

        var consumedAt = DateTime.UtcNow;

        Upshot consumeResult;
        try
        {
            consumeResult = await inventoryRepository.ConsumeReservationsAsync(
                reservations,
                consumedAt,
                cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            LogConcurrentReservationChange(logger, @event.WorkOrderId);
            throw;
        }
        if (consumeResult.IsFailure)
        {
            LogConsumptionFailed(logger, @event.WorkOrderId, consumeResult.Error.Message ?? "Unknown error");
            throw new InvalidOperationException(consumeResult.Error.Message);
        }

        var consumedLines = reservations
            .Select(r => new ConsumedInventoryLine(r.InventoryItemId, r.Quantity))
            .ToList();

        await bus.PublishAsync(new InventoryConsumedIntegrationEvent(
            @event.WorkOrderId,
            consumedLines,
            consumedAt)).ConfigureAwait(false);

        LogInventoryConsumed(logger, @event.WorkOrderId, consumedLines.Count);
    }

    private static async Task HandleCanceledWorkOrder(
        WorkOrderStatusChangedIntegrationEvent @event,
        IInventoryItemRepository inventoryRepository,
        IInventoryReservationRepository reservationRepository,
        ILogger<Marker> logger,
        CancellationToken cancellationToken)
    {
        var reservations = await reservationRepository.GetActiveReservationsByWorkOrderIdAsync(
            @event.WorkOrderId, cancellationToken).ConfigureAwait(false);

        if (reservations.Count == 0)
            return;

        Upshot releaseResult;
        try
        {
            releaseResult = await inventoryRepository.ReleaseReservationsAsync(
                reservations,
                DateTime.UtcNow,
                cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            LogConcurrentReservationChange(logger, @event.WorkOrderId);
            throw;
        }
        if (releaseResult.IsFailure)
        {
            LogReleaseFailed(logger, @event.WorkOrderId, releaseResult.Error.Message ?? "Unknown error");
            throw new InvalidOperationException(releaseResult.Error.Message);
        }

        LogReservationsReleased(logger, @event.WorkOrderId, reservations.Count);
    }

    /// <summary>Marker type used only to scope the <see cref="ILogger{TCategoryName}"/> category.</summary>
    public sealed class Marker
    {
        public static string Category => "WorkOrderStatusChangedInventory";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "No active reservations for WorkOrder {WorkOrderId}.")]
    private static partial void LogNoActiveReservations(ILogger logger, Guid workOrderId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to consume inventory reservations for WorkOrder {WorkOrderId}: {Error}.")]
    private static partial void LogConsumptionFailed(ILogger logger, Guid workOrderId, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to release inventory reservations for WorkOrder {WorkOrderId}: {Error}.")]
    private static partial void LogReleaseFailed(ILogger logger, Guid workOrderId, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Consumed {Count} inventory item(s) for WorkOrder {WorkOrderId}.")]
    private static partial void LogInventoryConsumed(ILogger logger, Guid workOrderId, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Released {Count} reservation(s) for canceled WorkOrder {WorkOrderId}.")]
    private static partial void LogReservationsReleased(ILogger logger, Guid workOrderId, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Inventory reservations for WorkOrder {WorkOrderId} changed concurrently.")]
    private static partial void LogConcurrentReservationChange(ILogger logger, Guid workOrderId);
}
