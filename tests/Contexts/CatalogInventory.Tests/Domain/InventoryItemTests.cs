namespace CatCar.Contexts.CatalogInventory.Tests.Domain;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.SharedKernel;
using FluentAssertions;
using Xunit;

public class InventoryItemTests
{
    [Fact]
    public void Register_WithValidData_ShouldSucceedAndNormalizeSku()
    {
        var result = InventoryItem.Register("oleo-5w30", "Óleo 5W30 sintético", "Óleo sintético 1 litro", 45m, 20, 5);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sku.Should().Be("OLEO-5W30");
        result.Value.Name.Should().Be("Óleo 5W30 sintético");
        result.Value.UnitPrice.Amount.Should().Be(45m);
        result.Value.QuantityInStock.Should().Be(20);
        result.Value.MinimumStockThreshold.Should().Be(5);
        result.Value.IsActive.Should().BeTrue();
        result.Value.IsBelowMinimumStock.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithoutSku_ShouldFail(string? sku)
    {
        var result = InventoryItem.Register(sku, "Filtro de óleo", "Filtro de óleo padrão", 20m, 10, 2);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Register_WithoutName_ShouldFail(string? name)
    {
        var result = InventoryItem.Register("FLT-001", name, "Filtro de óleo padrão", 20m, 10, 2);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_WithNegativeInitialQuantity_ShouldFail()
    {
        var result = InventoryItem.Register("FLT-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, -1, 2);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_WithNegativeMinimumStockThreshold_ShouldFail()
    {
        var result = InventoryItem.Register("FLT-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, 10, -1);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_WithQuantityBelowMinimum_ShouldFlagIsBelowMinimumStock()
    {
        var result = InventoryItem.Register("FLT-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, 1, 5);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsBelowMinimumStock.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateFieldsAndKeepQuantity()
    {
        var item = InventoryItem.Register("FLT-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, 10, 2).Value;

        var result = item.UpdateDetails("Filtro de óleo premium", "Filtro de óleo de alta performance", 25m, 3);

        result.IsSuccess.Should().BeTrue();
        item.Name.Should().Be("Filtro de óleo premium");
        item.Description.Should().Be("Filtro de óleo de alta performance");
        item.UnitPrice.Amount.Should().Be(25m);
        item.MinimumStockThreshold.Should().Be(3);
        item.QuantityInStock.Should().Be(10);
    }

    [Fact]
    public void AdjustStock_WithEntrada_ShouldIncreaseQuantity()
    {
        var item = InventoryItem.Register("FLT-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, 10, 2).Value;

        var result = item.AdjustStock(StockMovementType.Entrada, 5);

        result.IsSuccess.Should().BeTrue();
        item.QuantityInStock.Should().Be(15);
    }

    [Fact]
    public void AdjustStock_WithSaida_ShouldDecreaseQuantity()
    {
        var item = InventoryItem.Register("FLT-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, 10, 2).Value;

        var result = item.AdjustStock(StockMovementType.Saida, 4);

        result.IsSuccess.Should().BeTrue();
        item.QuantityInStock.Should().Be(6);
    }

    [Fact]
    public void AdjustStock_SaidaWithInsufficientStock_ShouldFailAndKeepQuantity()
    {
        var item = InventoryItem.Register("FLT-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, 3, 2).Value;

        var result = item.AdjustStock(StockMovementType.Saida, 5);

        result.IsFailure.Should().BeTrue();
        item.QuantityInStock.Should().Be(3);
    }

    [Fact]
    public void AdjustStock_WithZeroOrNegativeQuantity_ShouldFail()
    {
        var item = InventoryItem.Register("FLT-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, 10, 2).Value;

        var result = item.AdjustStock(StockMovementType.Entrada, 0);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldBecomeInactive()
    {
        var item = InventoryItem.Register("FLT-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, 10, 2).Value;

        item.Deactivate();

        item.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldThrow()
    {
        var item = InventoryItem.Register("FLT-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, 10, 2).Value;
        item.Deactivate();

        var act = () => item.Deactivate();

        act.Should().Throw<BusinessRuleViolatedException>();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrow()
    {
        var item = InventoryItem.Register("FLT-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, 10, 2).Value;

        var act = () => item.Activate();

        act.Should().Throw<BusinessRuleViolatedException>();
    }
}
