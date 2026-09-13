namespace CatCar.Contexts.ServiceOperations.Tests.Features.Customers;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Features.Customers.SetCustomerActiveStatus;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class SetCustomerActiveStatusHandlerTests
{
    private readonly SetCustomerActiveStatusCommandValidator _validator = new();
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();

    [Fact]
    public async Task Handle_WithExistingCustomer_ShouldToggleActiveStatus()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;
        var command = new SetCustomerActiveStatusCommand(customer.Id, false);
        _repository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await SetCustomerActiveStatusHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistingCustomer_ShouldFail()
    {
        var command = new SetCustomerActiveStatusCommand(Guid.CreateVersion7(), false);
        _repository.GetByIdAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await SetCustomerActiveStatusHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
