namespace CatCar.Contexts.CatalogInventory.Tests.Domain;

using CatCar.Contexts.CatalogInventory.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(19.9)]
    [InlineData(1250.755)]
    public void Create_WithValidAmount_ShouldSucceedAndDefaultToBrl(double amount)
    {
        var result = Money.Create((decimal)amount);

        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("BRL");
        result.Value.Amount.Should().Be(Math.Round((decimal)amount, 2, MidpointRounding.ToEven));
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldFail()
    {
        var result = Money.Create(-0.01m);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("R")]
    [InlineData("REAL")]
    public void Create_WithInvalidCurrency_ShouldFail(string currency)
    {
        var result = Money.Create(10m, currency);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithLowerCaseCurrency_ShouldNormalizeToUpperCase()
    {
        var result = Money.Create(10m, "brl");

        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("BRL");
    }

    [Fact]
    public void TwoMoneys_WithSameAmountAndCurrency_ShouldBeEqual()
    {
        var first = Money.Create(150m).Value;
        var second = Money.Create(150m).Value;

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void TwoMoneys_WithDifferentAmount_ShouldNotBeEqual()
    {
        var first = Money.Create(150m).Value;
        var second = Money.Create(151m).Value;

        (first == second).Should().BeFalse();
    }
}
