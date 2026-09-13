namespace CatCar.Contexts.ServiceOperations.Domain.WorkOrders;

/// <summary>
/// A service requested on a WorkOrder, referencing a CatalogedService from the CatalogInventory context by ID only,
/// with a snapshot of its description/price captured at inclusion time (per cross-BC boundary rules).
/// </summary>
public sealed class RequestedServiceLine
{
    public Guid Id { get; private set; }

    public Guid CatalogedServiceId { get; private set; }

    public string Description { get; private set; }

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    internal RequestedServiceLine(Guid catalogedServiceId, string description, decimal unitPrice, int quantity)
    {
        Id = Guid.CreateVersion7();
        CatalogedServiceId = catalogedServiceId;
        Description = description;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

#pragma warning disable CS8618
    private RequestedServiceLine()
    {
    }
#pragma warning restore CS8618
}
