namespace CatCar.Contexts.ServiceOperations.Tests.Features.Customers;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerByDocument;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class GetCustomerByDocumentHandlerTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();

    [Fact]
    public async Task Handle_WithExistingDocument_ShouldReturnDetails()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;
        _repository.GetByDocumentAsync("52998224725", Arg.Any<CancellationToken>()).Returns(customer);

        var result = await GetCustomerByDocumentHandler.Handle(new GetCustomerByDocumentQuery("529.982.247-25"), _repository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("João da Silva");
    }

    [Fact]
    public async Task Handle_WithoutDocument_ShouldFailWithoutHittingRepository()
    {
        var result = await GetCustomerByDocumentHandler.Handle(new GetCustomerByDocumentQuery(string.Empty), _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().GetByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownDocument_ShouldFail()
    {
        _repository.GetByDocumentAsync("52998224725", Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await GetCustomerByDocumentHandler.Handle(new GetCustomerByDocumentQuery("52998224725"), _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
