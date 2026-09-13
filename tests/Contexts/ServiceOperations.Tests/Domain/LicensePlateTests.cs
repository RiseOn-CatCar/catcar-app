namespace CatCar.Contexts.ServiceOperations.Tests.Domain;

using CatCar.Contexts.ServiceOperations.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class LicensePlateTests
{
    [Theory]
    [InlineData("ABC1234")]
    [InlineData("abc-1234")]
    public void Create_WithValidLegacyFormat_ShouldSucceed(string rawValue)
    {
        var result = LicensePlate.Create(rawValue);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("ABC1234");
    }

    [Theory]
    [InlineData("ABC1D23")]
    [InlineData("abc-1d23")]
    public void Create_WithValidMercosulFormat_ShouldSucceed(string rawValue)
    {
        var result = LicensePlate.Create(rawValue);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("ABC1D23");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutValue_ShouldFail(string? rawValue)
    {
        var result = LicensePlate.Create(rawValue);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("AB1234")]
    [InlineData("ABCD1234")]
    [InlineData("ABC12345")]
    public void Create_WithInvalidFormat_ShouldFail(string rawValue)
    {
        var result = LicensePlate.Create(rawValue);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnNormalizedValue()
    {
        var plate = LicensePlate.Create("abc-1234").Value;

        plate.ToString().Should().Be("ABC1234");
    }
}
