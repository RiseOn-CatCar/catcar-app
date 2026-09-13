namespace CatCar.Contexts.CatalogInventory.Tests.Features.InventoryItems;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Features.InventoryItems.RegisterInventoryItem;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class RegisterInventoryItemHandlerTests
{
    private readonly RegisterInventoryItemCommandValidator _validator = new();
    private readonly IInventoryItemRepository _repository = Substitute.For<IInventoryItemRepository>();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldRegisterItem()
    {
        var command = new RegisterInventoryItemCommand("flt-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, 10, 2);
        _repository.ExistsBySkuAsync("FLT-001", Arg.Any<CancellationToken>()).Returns(false);

        var result = await RegisterInventoryItemHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sku.Should().Be("FLT-001");
        result.Value.QuantityInStock.Should().Be(10);
        await _repository.Received(1).AddAsync(Arg.Any<InventoryItem>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateSku_ShouldFailAndNotPersist()
    {
        var command = new RegisterInventoryItemCommand("FLT-001", "Filtro de óleo", "Filtro de óleo padrão", 20m, 10, 2);
        _repository.ExistsBySkuAsync("FLT-001", Arg.Any<CancellationToken>()).Returns(true);

        var result = await RegisterInventoryItemHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().AddAsync(Arg.Any<InventoryItem>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "Filtro de óleo", "Descrição", 20, 10, 2)]
    [InlineData("FLT-001", "", "Descrição", 20, 10, 2)]
    [InlineData("FLT-001", "Filtro de óleo", "", 20, 10, 2)]
    [InlineData("FLT-001", "Filtro de óleo", "Descrição", -1, 10, 2)]
    [InlineData("FLT-001", "Filtro de óleo", "Descrição", 20, -1, 2)]
    [InlineData("FLT-001", "Filtro de óleo", "Descrição", 20, 10, -1)]
    public async Task Handle_WithInvalidCommand_ShouldFailValidationWithoutHittingRepository(
        string sku, string name, string description, decimal unitPrice, int initialQuantity, int minimumStockThreshold)
    {
        var command = new RegisterInventoryItemCommand(sku, name, description, unitPrice, initialQuantity, minimumStockThreshold);

        var result = await RegisterInventoryItemHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().ExistsBySkuAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
