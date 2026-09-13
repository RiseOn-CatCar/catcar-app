namespace CatCar.Contexts.CatalogInventory.Tests.Features.InventoryItems;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Features.InventoryItems.AdjustInventoryStock;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class AdjustInventoryStockHandlerTests
{
    private readonly AdjustInventoryStockCommandValidator _validator = new();
    private readonly IInventoryItemRepository _repository = Substitute.For<IInventoryItemRepository>();

    private static InventoryItem CreateItem(int quantity = 10, int minimum = 2) =>
        InventoryItem.Register("FLT-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, quantity, minimum).Value;

    [Fact]
    public async Task Handle_WithEntrada_ShouldIncreaseStockAndPersist()
    {
        var item = CreateItem();
        var command = new AdjustInventoryStockCommand(item.Id, "Entrada", 5, "Reposição de fornecedor");
        _repository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await AdjustInventoryStockHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.QuantityInStock.Should().Be(15);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSaidaExceedingStock_ShouldFailAndNotPersist()
    {
        var item = CreateItem(quantity: 3);
        var command = new AdjustInventoryStockCommand(item.Id, "Saida", 5, "Consumo em OS");
        _repository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await AdjustInventoryStockHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenItemNotFound_ShouldFail()
    {
        var itemId = Guid.CreateVersion7();
        var command = new AdjustInventoryStockCommand(itemId, "Entrada", 5, null);
        _repository.GetByIdAsync(itemId, Arg.Any<CancellationToken>()).Returns((InventoryItem?)null);

        var result = await AdjustInventoryStockHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("Invalida", 5)]
    [InlineData("Entrada", 0)]
    [InlineData("Entrada", -1)]
    public async Task Handle_WithInvalidCommand_ShouldFailValidationWithoutHittingRepository(string movementType, int quantity)
    {
        var command = new AdjustInventoryStockCommand(Guid.CreateVersion7(), movementType, quantity, null);

        var result = await AdjustInventoryStockHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
