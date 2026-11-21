namespace CatCar.Contexts.ServiceOperations.Tests;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Features.Customers.LookupCustomerForAuthentication;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

public sealed class LookupCustomerForAuthenticationTests
{
    private const string Key = "unit-test-internal-key";
    private const string Document = "52998224725";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_ExistingCustomer_ReturnsIdentityAndStatus(bool active)
    {
        var customer = Customer.Register(Document, "Customer", "85999990000", null).Value;
        customer.SetActiveStatus(active);
        var repository = Substitute.For<ICustomerRepository>();
        repository.GetByDocumentAsync(Document, Arg.Any<CancellationToken>()).Returns(customer);
        var result = await Invoke(repository, Key, Key);
        ((IStatusCodeHttpResult)result).StatusCode.Should().Be(StatusCodes.Status200OK);
        var value = ((IValueHttpResult)result).Value.Should().BeOfType<AuthenticationCustomer>().Subject;
        value.Id.Should().Be(customer.Id);
        value.IsActive.Should().Be(active);
    }

    [Fact]
    public async Task HandleAsync_UnknownCustomer_ReturnsNotFound()
    {
        var result = await Invoke(Substitute.For<ICustomerRepository>(), Key, Key);
        ((IStatusCodeHttpResult)result).StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Theory]
    [InlineData(Key, null)]
    [InlineData(Key, "wrong")]
    [InlineData(null, Key)]
    [InlineData("", Key)]
    public async Task HandleAsync_InvalidKey_ReturnsUnauthorized(string? configured, string? supplied)
    {
        var repository = Substitute.For<ICustomerRepository>();
        var result = await Invoke(repository, configured, supplied);
        ((IStatusCodeHttpResult)result).StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        await repository.DidNotReceiveWithAnyArgs().GetByDocumentAsync(default!, default);
    }

    private static Task<IResult> Invoke(ICustomerRepository repository, string? configured, string? supplied)
    {
        var context = new DefaultHttpContext();
        if (supplied is not null)
            context.Request.Headers["X-Internal-Api-Key"] = supplied;
        return LookupCustomerForAuthenticationEndpoint.HandleAsync(Document, context.Request, Options.Create(new InternalApiOptions { Key = configured }), repository, CancellationToken.None);
    }
}
