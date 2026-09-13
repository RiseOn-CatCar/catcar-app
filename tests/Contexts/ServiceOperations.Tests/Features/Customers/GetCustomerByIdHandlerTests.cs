namespace CatCar.Contexts.ServiceOperations.Tests.Features.Customers;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerById;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class GetCustomerByIdHandlerTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();

    [Fact]
    public async Task Handle_WithExistingCustomer_ShouldReturnDetails()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;
        _repository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await GetCustomerByIdHandler.Handle(new GetCustomerByIdQuery(customer.Id), _repository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("João da Silva");
    }

    [Fact]
    public async Task Handle_WithNonExistingCustomer_ShouldFail()
    {
        var customerId = Guid.CreateVersion7();
        _repository.GetByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await GetCustomerByIdHandler.Handle(new GetCustomerByIdQuery(customerId), _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
