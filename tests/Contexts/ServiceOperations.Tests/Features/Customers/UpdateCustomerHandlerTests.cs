namespace CatCar.Contexts.ServiceOperations.Tests.Features.Customers;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Features.Customers.UpdateCustomer;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class UpdateCustomerHandlerTests
{
    private readonly UpdateCustomerCommandValidator _validator = new();
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();

    [Fact]
    public async Task Handle_WithExistingCustomer_ShouldUpdateContactDetails()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;
        var command = new UpdateCustomerCommand(customer.Id, "João S. Silva", "(85) 98888-2222", "joao.silva@example.com");
        _repository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await UpdateCustomerHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("João S. Silva");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistingCustomer_ShouldFail()
    {
        var command = new UpdateCustomerCommand(Guid.CreateVersion7(), "João S. Silva", "(85) 98888-2222", null);
        _repository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await UpdateCustomerHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidCommand_ShouldFailValidationWithoutHittingRepository()
    {
        var command = new UpdateCustomerCommand(Guid.CreateVersion7(), string.Empty, "(85) 98888-2222", null);

        var result = await UpdateCustomerHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
