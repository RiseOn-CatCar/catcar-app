namespace CatCar.Contexts.ServiceOperations.Tests.Domain;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using FluentAssertions;
using Xunit;

public class CustomerTests
{
    [Fact]
    public void Register_WithValidData_ShouldSucceedAndBeActive()
    {
        var result = Customer.Register("529.982.247-25", "João da Silva", "(85) 99999-0000", "joao@example.com");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("João da Silva");
        result.Value.Phone.Should().Be("(85) 99999-0000");
        result.Value.Email.Should().Be("joao@example.com");
        result.Value.IsActive.Should().BeTrue();
        result.Value.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Register_WithoutEmail_ShouldSucceedWithNullEmail()
    {
        var result = Customer.Register("52998224725", "Maria Souza", "(85) 98888-1111", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().BeNull();
    }

    [Fact]
    public void Register_WithInvalidDocument_ShouldFail()
    {
        var result = Customer.Register("123", "João da Silva", "(85) 99999-0000", null);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithoutName_ShouldFail(string? name)
    {
        var result = Customer.Register("52998224725", name, "(85) 99999-0000", null);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Register_WithoutPhone_ShouldFail(string? phone)
    {
        var result = Customer.Register("52998224725", "João da Silva", phone, null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateContactDetails_WithValidData_ShouldUpdateFields()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;

        var result = customer.UpdateContactDetails("João S. Silva", "(85) 98888-2222", "joao.silva@example.com");

        result.IsSuccess.Should().BeTrue();
        customer.Name.Should().Be("João S. Silva");
        customer.Phone.Should().Be("(85) 98888-2222");
        customer.Email.Should().Be("joao.silva@example.com");
    }

    [Fact]
    public void UpdateContactDetails_WithInvalidData_ShouldFailAndKeepOriginalState()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;

        var result = customer.UpdateContactDetails(string.Empty, "(85) 98888-2222", null);

        result.IsFailure.Should().BeTrue();
        customer.Name.Should().Be("João da Silva");
    }

    [Fact]
    public void SetActiveStatus_ShouldToggleIsActive()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;

        customer.SetActiveStatus(false);
        customer.IsActive.Should().BeFalse();

        customer.SetActiveStatus(true);
        customer.IsActive.Should().BeTrue();
    }
}
