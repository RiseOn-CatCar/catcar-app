namespace CatCar.Contexts.CatalogInventory.Domain.InventoryItems;

using CatCar.Contexts.CatalogInventory.Domain.ValueObjects;
using CatCar.SharedKernel;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Aggregate root representing a part or supply (peça/insumo) managed by the workshop's stock.
/// AC: CRUD de peças e insumos, com controle de estoque (feature 03).
/// Aliases (forbidden per glossary): "produto".
/// </summary>
public sealed class InventoryItem : Entity<Guid>, IAggregateRoot
{
    public string Sku { get; private set; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public Money UnitPrice { get; private set; }

    public int QuantityInStock { get; private set; }

    public int MinimumStockThreshold { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsBelowMinimumStock => QuantityInStock < MinimumStockThreshold;

    private InventoryItem(
        Guid id,
        string sku,
        string name,
        string description,
        Money unitPrice,
        int quantityInStock,
        int minimumStockThreshold)
        : base(id)
    {
        Sku = sku;
        Name = name;
        Description = description;
        UnitPrice = unitPrice;
        QuantityInStock = quantityInStock;
        MinimumStockThreshold = minimumStockThreshold;
        IsActive = true;
    }

    /// <summary>
    /// Registers a new part/supply with an initial stock quantity.
    /// </summary>
    public static Upshot<InventoryItem> Register(
        string? sku,
        string? name,
        string? description,
        decimal unitPriceAmount,
        int initialQuantity,
        int minimumStockThreshold)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return Upshot<InventoryItem>.Fail("O código (SKU) é obrigatório.");

        if (sku.Trim().Length > 40)
            return Upshot<InventoryItem>.Fail("O código (SKU) deve ter no máximo 40 caracteres.");

        var validation = ValidateDetails(name, description, minimumStockThreshold);
        if (validation.IsFailure)
            return Upshot<InventoryItem>.Fail(validation.Error);

        if (initialQuantity < 0)
            return Upshot<InventoryItem>.Fail("A quantidade inicial em estoque não pode ser negativa.");

        var priceResult = Money.Create(unitPriceAmount);
        if (priceResult.IsFailure)
            return Upshot<InventoryItem>.Fail(priceResult.Error);

        var item = new InventoryItem(
            Guid.CreateVersion7(),
            sku.Trim().ToUpperInvariant(),
            name!.Trim(),
            description!.Trim(),
            priceResult.Value,
            initialQuantity,
            minimumStockThreshold);

        return Upshot<InventoryItem>.Success(item);
    }

    /// <summary>
    /// Updates the descriptive, pricing and minimum-stock details. Does not change the current stock quantity -
    /// use <see cref="AdjustStock"/> for that.
    /// </summary>
    public Upshot UpdateDetails(string? name, string? description, decimal unitPriceAmount, int minimumStockThreshold)
    {
        var validation = ValidateDetails(name, description, minimumStockThreshold);
        if (validation.IsFailure)
            return Upshot.Fail(validation.Error);

        var priceResult = Money.Create(unitPriceAmount);
        if (priceResult.IsFailure)
            return Upshot.Fail(priceResult.Error);

        Name = name!.Trim();
        Description = description!.Trim();
        UnitPrice = priceResult.Value;
        MinimumStockThreshold = minimumStockThreshold;
        return Upshot.Success();
    }

    /// <summary>
    /// Applies a stock movement (entrada/saída), enforcing that stock never goes negative.
    /// </summary>
    public Upshot AdjustStock(StockMovementType movementType, int quantity)
    {
        if (quantity <= 0)
            return Upshot.Fail("A quantidade da movimentação deve ser maior que zero.");

        if (movementType == StockMovementType.Saida && quantity > QuantityInStock)
            return Upshot.Fail(
                $"Estoque insuficiente para '{Name}'. Disponível: {QuantityInStock}, solicitado: {quantity}.");

        QuantityInStock = movementType == StockMovementType.Entrada
            ? QuantityInStock + quantity
            : QuantityInStock - quantity;

        return Upshot.Success();
    }

    /// <summary>
    /// Activates a previously deactivated part/supply, making it available for new work orders.
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            throw new BusinessRuleViolatedException("A peça/insumo já está ativa.");

        IsActive = true;
    }

    /// <summary>
    /// Deactivates the part/supply, preventing it from being used in new work orders.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            throw new BusinessRuleViolatedException("A peça/insumo já está inativa.");

        IsActive = false;
    }

    private static Upshot ValidateDetails(string? name, string? description, int minimumStockThreshold)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Upshot.Fail("O nome da peça/insumo é obrigatório.");

        if (name.Trim().Length > 150)
            return Upshot.Fail("O nome deve ter no máximo 150 caracteres.");

        if (string.IsNullOrWhiteSpace(description))
            return Upshot.Fail("A descrição é obrigatória.");

        if (description.Trim().Length > 500)
            return Upshot.Fail("A descrição deve ter no máximo 500 caracteres.");

        if (minimumStockThreshold < 0)
            return Upshot.Fail("O estoque mínimo não pode ser negativo.");

        return Upshot.Success();
    }
}
