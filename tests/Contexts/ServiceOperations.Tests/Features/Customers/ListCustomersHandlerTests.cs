namespace CatCar.Contexts.ServiceOperations.Tests.Features.Customers;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Features.Customers.ListCustomers;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class ListCustomersHandlerTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();

    [Fact]
    public async Task Handle_ShouldReturnSummariesFromRepository()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;
        _repository.ListAsync(null, Arg.Any<CancellationToken>()).Returns(new List<Customer> { customer });

        var result = await ListCustomersHandler.Handle(new ListCustomersQuery(null), _repository, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("João da Silva");
    }

    [Fact]
    public async Task Handle_WithOnlyActiveFilter_ShouldForwardToRepository()
    {
        _repository.ListAsync(true, Arg.Any<CancellationToken>()).Returns(new List<Customer>());

        await ListCustomersHandler.Handle(new ListCustomersQuery(true), _repository, CancellationToken.None);

        await _repository.Received(1).ListAsync(true, Arg.Any<CancellationToken>());
    }
}
