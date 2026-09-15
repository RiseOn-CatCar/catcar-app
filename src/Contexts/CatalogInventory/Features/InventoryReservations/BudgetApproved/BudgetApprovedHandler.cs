namespace CatCar.Contexts.CatalogInventory.Features.InventoryReservations.BudgetApproved;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using Microsoft.Extensions.Logging;
using Wolverine;

/// <summary>
/// Wolverine handler consuming <see cref="BudgetApprovedIntegrationEvent"/>.
/// Reserves available inventory for approved budget lines and publishes <see cref="InventoryReservedIntegrationEvent"/>.
/// </summary>
public static partial class BudgetApprovedHandler
{
    public static async Task Handle(
        BudgetApprovedIntegrationEvent @event,
        IMessageBus bus,
        IInventoryItemRepository inventoryRepository,
        IInventoryReservationRepository reservationRepository,
        ILogger<Marker> logger,
        CancellationToken cancellationToken)
    {
        var budgetSnapshot = await bus.InvokeAsync<BudgetSnapshotResponse>(
            new GetBudgetSnapshotQuery(@event.BudgetId),
            cancellationToken).ConfigureAwait(false);

        if (!budgetSnapshot.Found)
        {
            LogBudgetNotFound(logger, @event.BudgetId);
            return;
        }

        var partLines = budgetSnapshot.Lines
            .Where(l => string.Equals(l.Type, "Part", StringComparison.OrdinalIgnoreCase) && l.ReferenceId != Guid.Empty)
            .GroupBy(l => l.ReferenceId)
            .Select(g => new { InventoryItemId = g.Key, Quantity = g.Sum(l => l.Quantity) })
            .ToList();

        if (partLines.Count == 0)
        {
            LogNoPartLines(logger, @event.BudgetId);
            return;
        }

        var existingReservations = await reservationRepository.GetActiveReservationsByBudgetIdAsync(
            @event.BudgetId, cancellationToken).ConfigureAwait(false);
        if (existingReservations.Count > 0)
        {
            LogReservationsAlreadyExist(logger, @event.BudgetId);
            return;
        }

        var reservations = new List<InventoryReservation>(partLines.Count);
        var reservedLines = new List<ReservedInventoryLine>(partLines.Count);
        var reservedAt = DateTime.UtcNow;

        foreach (var line in partLines)
        {
            var reservationResult = InventoryReservation.Create(
                @event.WorkOrderId,
                @event.BudgetId,
                line.InventoryItemId,
                line.Quantity,
                reservedAt);

            if (reservationResult.IsFailure)
            {
                LogReservationFailed(logger, line.InventoryItemId, reservationResult.Error.Message ?? "Unknown error");
                throw new InvalidOperationException(reservationResult.Error.Message);
            }

            reservations.Add(reservationResult.Value);
            reservedLines.Add(new ReservedInventoryLine(line.InventoryItemId, line.Quantity));
        }

        var reserveResult = await inventoryRepository.ReserveAsync(reservations, cancellationToken).ConfigureAwait(false);
        if (reserveResult.IsFailure)
        {
            LogInventoryReservationFailed(logger, @event.BudgetId, reserveResult.Error.Message ?? "Unknown error");
            throw new InvalidOperationException(reserveResult.Error.Message);
        }

        await bus.PublishAsync(new InventoryReservedIntegrationEvent(
            @event.WorkOrderId,
            @event.BudgetId,
            reservedLines,
            reservedAt)).ConfigureAwait(false);

        LogInventoryReserved(logger, @event.WorkOrderId, @event.BudgetId, reservations.Count);
    }

    /// <summary>Marker type used only to scope the <see cref="ILogger{TCategoryName}"/> category.</summary>
    public sealed class Marker
    {
        public static string Category => "BudgetApproved";
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Budget {BudgetId} not found when reserving inventory.")]
    private static partial void LogBudgetNotFound(ILogger logger, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Budget {BudgetId} has no part lines to reserve.")]
    private static partial void LogNoPartLines(ILogger logger, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Inventory is already reserved for Budget {BudgetId}.")]
    private static partial void LogReservationsAlreadyExist(ILogger logger, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to create reservation for item {InventoryItemId}: {Error}.")]
    private static partial void LogReservationFailed(ILogger logger, Guid inventoryItemId, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to reserve inventory for Budget {BudgetId}: {Error}.")]
    private static partial void LogInventoryReservationFailed(ILogger logger, Guid budgetId, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reserved {Count} inventory item(s) for WorkOrder {WorkOrderId}, Budget {BudgetId}.")]
    private static partial void LogInventoryReserved(ILogger logger, Guid workOrderId, Guid budgetId, int count);
}
