namespace CatCar.Contexts.CatalogInventory.Domain.InventoryItems;

using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Persistence port for <see cref="InventoryItem"/> aggregates.
/// Implemented in Infrastructure and registered via RiseOn.AutoInject.
/// </summary>
public interface IInventoryItemRepository
{
    Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryItem>> ListAsync(bool? onlyActive, bool? onlyBelowMinimumStock, CancellationToken cancellationToken = default);

    Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default);

    Task<Upshot> ReserveAsync(
        IReadOnlyCollection<InventoryReservation> reservations,
        CancellationToken cancellationToken = default);

    Task<Upshot> ReleaseReservationsAsync(
        IReadOnlyCollection<InventoryReservation> reservations,
        DateTime releasedAt,
        CancellationToken cancellationToken = default);

    Task<Upshot> ConsumeReservationsAsync(
        IReadOnlyCollection<InventoryReservation> reservations,
        DateTime consumedAt,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
