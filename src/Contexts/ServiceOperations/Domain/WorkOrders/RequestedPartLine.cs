namespace CatCar.Contexts.ServiceOperations.Domain.WorkOrders;

/// <summary>
/// A part/supply requested on a WorkOrder, referencing an InventoryItem from the CatalogInventory context by ID
/// only, with a snapshot of its description/price captured at inclusion time (per cross-BC boundary rules).
/// </summary>
public sealed class RequestedPartLine
{
    public Guid Id { get; private set; }

    public Guid InventoryItemId { get; private set; }

    public string Description { get; private set; }

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    internal RequestedPartLine(Guid inventoryItemId, string description, decimal unitPrice, int quantity)
    {
        Id = Guid.CreateVersion7();
        InventoryItemId = inventoryItemId;
        Description = description;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

#pragma warning disable CS8618
    private RequestedPartLine()
    {
    }
#pragma warning restore CS8618
}
