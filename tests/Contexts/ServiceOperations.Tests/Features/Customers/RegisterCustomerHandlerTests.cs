namespace CatCar.Contexts.ServiceOperations.Tests.Features.Customers;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Features.Customers.RegisterCustomer;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class RegisterCustomerHandlerTests
{
    private readonly RegisterCustomerCommandValidator _validator = new();
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldRegisterCustomer()
    {
        var command = new RegisterCustomerCommand("52998224725", "João da Silva", "(85) 99999-0000", "joao@example.com");
        _repository.ExistsByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await RegisterCustomerHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("João da Silva");
        result.Value.IsActive.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateDocument_ShouldFailAndNotPersist()
    {
        var command = new RegisterCustomerCommand("52998224725", "João da Silva", "(85) 99999-0000", null);
        _repository.ExistsByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await RegisterCustomerHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidDocument_ShouldFailWithoutHittingRepository()
    {
        var command = new RegisterCustomerCommand("123", "João da Silva", "(85) 99999-0000", null);

        var result = await RegisterCustomerHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().ExistsByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "João da Silva", "(85) 99999-0000")]
    [InlineData("52998224725", "", "(85) 99999-0000")]
    [InlineData("52998224725", "João da Silva", "")]
    public async Task Handle_WithInvalidCommand_ShouldFailValidation(string document, string name, string phone)
    {
        var command = new RegisterCustomerCommand(document, name, phone, null);

        var result = await RegisterCustomerHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().ExistsByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
