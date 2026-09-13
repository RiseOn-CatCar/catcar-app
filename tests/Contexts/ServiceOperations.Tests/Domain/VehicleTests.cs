namespace CatCar.Contexts.ServiceOperations.Tests.Domain;

using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using FluentAssertions;
using Xunit;

public class VehicleTests
{
    private static readonly Guid CustomerId = Guid.CreateVersion7();

    [Fact]
    public void Register_WithValidData_ShouldSucceedAndBeActive()
    {
        var result = Vehicle.Register(CustomerId, "ABC1234", "Fiat", "Uno", 2015);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be(CustomerId);
        result.Value.Plate.Value.Should().Be("ABC1234");
        result.Value.Brand.Should().Be("Fiat");
        result.Value.Model.Should().Be("Uno");
        result.Value.ManufactureYear.Should().Be(2015);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Register_WithoutCustomerId_ShouldFail()
    {
        var result = Vehicle.Register(Guid.Empty, "ABC1234", "Fiat", "Uno", 2015);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_WithInvalidPlate_ShouldFail()
    {
        var result = Vehicle.Register(CustomerId, "INVALID", "Fiat", "Uno", 2015);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Register_WithoutBrand_ShouldFail(string? brand)
    {
        var result = Vehicle.Register(CustomerId, "ABC1234", brand, "Uno", 2015);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_WithFutureManufactureYear_ShouldFail()
    {
        var result = Vehicle.Register(CustomerId, "ABC1234", "Fiat", "Uno", DateTime.UtcNow.Year + 5);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateFieldsAndKeepPlate()
    {
        var vehicle = Vehicle.Register(CustomerId, "ABC1234", "Fiat", "Uno", 2015).Value;

        var result = vehicle.UpdateDetails("Fiat", "Uno Mille", 2016);

        result.IsSuccess.Should().BeTrue();
        vehicle.Model.Should().Be("Uno Mille");
        vehicle.ManufactureYear.Should().Be(2016);
        vehicle.Plate.Value.Should().Be("ABC1234");
    }

    [Fact]
    public void UpdateDetails_WithInvalidData_ShouldFailAndKeepOriginalState()
    {
        var vehicle = Vehicle.Register(CustomerId, "ABC1234", "Fiat", "Uno", 2015).Value;

        var result = vehicle.UpdateDetails(string.Empty, "Uno Mille", 2016);

        result.IsFailure.Should().BeTrue();
        vehicle.Model.Should().Be("Uno");
    }

    [Fact]
    public void SetActiveStatus_ShouldToggleIsActive()
    {
        var vehicle = Vehicle.Register(CustomerId, "ABC1234", "Fiat", "Uno", 2015).Value;

        vehicle.SetActiveStatus(false);
        vehicle.IsActive.Should().BeFalse();
    }
}
