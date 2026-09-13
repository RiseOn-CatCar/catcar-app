namespace CatCar.Contexts.CatalogInventory.Domain.ValueObjects;

using CatCar.SharedKernel;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Value object representing a monetary amount in a given currency.
/// Used for service prices and part/supply unit prices within the CatalogInventory context.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }

    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Creates a validated monetary amount. Defaults to Brazilian Real (BRL).
    /// </summary>
    public static Upshot<Money> Create(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
            return Upshot<Money>.Fail("O valor monetário não pode ser negativo.");

        if (string.IsNullOrWhiteSpace(currency))
            return Upshot<Money>.Fail("A moeda é obrigatória.");

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != 3)
            return Upshot<Money>.Fail("A moeda deve ser informada no formato ISO 4217 (ex.: BRL).");

        return Upshot<Money>.Success(new Money(Math.Round(amount, 2, MidpointRounding.ToEven), normalizedCurrency));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Currency} {Amount:0.00}";
}
