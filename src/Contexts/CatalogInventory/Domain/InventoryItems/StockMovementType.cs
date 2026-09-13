namespace CatCar.Contexts.CatalogInventory.Domain.InventoryItems;

/// <summary>
/// Type of stock movement applied to an <see cref="InventoryItem"/>.
/// </summary>
public enum StockMovementType
{
    /// <summary>Stock entry (e.g. purchase/replenishment).</summary>
    Entrada = 1,

    /// <summary>Stock exit (e.g. consumption by a work order).</summary>
    Saida = 2,
}
