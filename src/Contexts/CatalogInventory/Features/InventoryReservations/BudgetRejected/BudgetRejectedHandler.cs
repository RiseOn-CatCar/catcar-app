namespace CatCar.Contexts.CatalogInventory.Features.InventoryReservations.BudgetRejected;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contracts.ServiceOperations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler consuming <see cref="BudgetRejectedIntegrationEvent"/>.
/// Releases previously reserved inventory associated with the rejected budget.
/// </summary>
public static partial class BudgetRejectedHandler
{
    public static async Task Handle(
        BudgetRejectedIntegrationEvent @event,
        IInventoryItemRepository inventoryRepository,
        IInventoryReservationRepository reservationRepository,
        ILogger<Marker> logger,
        CancellationToken cancellationToken)
    {
        var reservations = await reservationRepository.GetActiveReservationsByBudgetIdAsync(
            @event.BudgetId, cancellationToken).ConfigureAwait(false);

        if (reservations.Count == 0)
        {
            LogNoActiveReservations(logger, @event.BudgetId);
            return;
        }

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
            LogConcurrentReservationChange(logger, @event.BudgetId);
            throw;
        }
        if (releaseResult.IsFailure)
        {
            LogReleaseFailed(logger, @event.BudgetId, releaseResult.Error.Message ?? "Unknown error");
            throw new InvalidOperationException(releaseResult.Error.Message);
        }

        LogReservationsReleased(logger, @event.BudgetId, reservations.Count);
    }

    /// <summary>Marker type used only to scope the <see cref="ILogger{TCategoryName}"/> category.</summary>
    public sealed class Marker
    {
        public static string Category => "BudgetRejected";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "No active reservations found for rejected Budget {BudgetId}.")]
    private static partial void LogNoActiveReservations(ILogger logger, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to release inventory for Budget {BudgetId}: {Error}.")]
    private static partial void LogReleaseFailed(ILogger logger, Guid budgetId, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Inventory reservations for Budget {BudgetId} changed concurrently.")]
    private static partial void LogConcurrentReservationChange(ILogger logger, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Released {Count} reservation(s) for rejected Budget {BudgetId}.")]
    private static partial void LogReservationsReleased(ILogger logger, Guid budgetId, int count);
}
