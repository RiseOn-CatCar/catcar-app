namespace CatCar.Contexts.CatalogInventory.Tests.Features.CatalogedServices;

using CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;
using CatCar.Contexts.CatalogInventory.Features.CatalogedServices.RegisterCatalogedService;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class RegisterCatalogedServiceHandlerTests
{
    private readonly RegisterCatalogedServiceCommandValidator _validator = new();
    private readonly ICatalogedServiceRepository _repository = Substitute.For<ICatalogedServiceRepository>();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldRegisterService()
    {
        var command = new RegisterCatalogedServiceCommand("Troca de óleo", "Troca de óleo do motor e filtro", 60, 150m);
        _repository.ExistsByNameAsync(command.Name, Arg.Any<CancellationToken>()).Returns(false);

        var result = await RegisterCatalogedServiceHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Troca de óleo");
        result.Value.IsActive.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<CatalogedService>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ShouldFailAndNotPersist()
    {
        var command = new RegisterCatalogedServiceCommand("Troca de óleo", "Troca de óleo do motor e filtro", 60, 150m);
        _repository.ExistsByNameAsync(command.Name, Arg.Any<CancellationToken>()).Returns(true);

        var result = await RegisterCatalogedServiceHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().AddAsync(Arg.Any<CatalogedService>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "Descrição válida", 60, 150)]
    [InlineData("Troca de óleo", "", 60, 150)]
    [InlineData("Troca de óleo", "Descrição válida", 0, 150)]
    [InlineData("Troca de óleo", "Descrição válida", 60, -1)]
    public async Task Handle_WithInvalidCommand_ShouldFailValidationWithoutHittingRepository(string name, string description, int duration, decimal price)
    {
        var command = new RegisterCatalogedServiceCommand(name, description, duration, price);

        var result = await RegisterCatalogedServiceHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().ExistsByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
