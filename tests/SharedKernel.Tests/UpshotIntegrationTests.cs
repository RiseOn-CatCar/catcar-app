namespace CatCar.SharedKernel.Tests;

using FluentAssertions;
using RiseOn.RailResult.Upshot;
using Xunit;

public class UpshotIntegrationTests
{
    // covers: AC-007
    [Fact]
    public void Upshot_Success_CreatesSuccessfulResult()
    {
        // Arrange & Act
        var result = Upshot.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // covers: AC-007
    [Fact]
    public void Upshot_Fail_CreatesFailedResult()
    {
        // Arrange & Act
        var result = Upshot.Fail("Test error");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    // covers: AC-007
    [Fact]
    public void UpshotGeneric_Success_CreatesSuccessfulResultWithValue()
    {
        // Arrange & Act
        var result = Upshot<int>.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    // covers: AC-007
    [Fact]
    public void UpshotGeneric_Fail_CreatesFailedResultWithError()
    {
        // Arrange & Act
        var result = Upshot<string>.Fail("Something went wrong");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.Message.Should().Be("Something went wrong");
    }

    // covers: AC-007
    [Fact]
    public void UpshotGeneric_FailWithException_CreatesFailedResult()
    {
        // Arrange
        var exception = new InvalidOperationException("Domain error");

        // Act
        var result = Upshot<int>.Fail(exception);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.Message.Should().Be("Domain error");
    }

    // covers: AC-007
    [Fact]
    public void Upshot_Success_IsSuccess_ReturnsTrue()
    {
        // Arrange
        var success = Upshot.Success();

        // Act & Assert
        success.IsSuccess.Should().BeTrue();
    }

    // covers: AC-007
    [Fact]
    public void Upshot_Fail_IsSuccess_ReturnsFalse()
    {
        // Arrange
        var fail = Upshot.Fail("error");

        // Act & Assert
        fail.IsSuccess.Should().BeFalse();
    }
}
