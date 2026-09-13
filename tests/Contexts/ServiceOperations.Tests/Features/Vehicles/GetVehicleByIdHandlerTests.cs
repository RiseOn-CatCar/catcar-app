namespace CatCar.Contexts.ServiceOperations.Tests.Features.Vehicles;

using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.GetVehicleById;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class GetVehicleByIdHandlerTests
{
    private readonly IVehicleRepository _repository = Substitute.For<IVehicleRepository>();

    [Fact]
    public async Task Handle_WithExistingVehicle_ShouldReturnDetails()
    {
        var vehicle = Vehicle.Register(Guid.CreateVersion7(), "ABC1234", "Fiat", "Uno", 2015).Value;
        _repository.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await GetVehicleByIdHandler.Handle(new GetVehicleByIdQuery(vehicle.Id), _repository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Plate.Should().Be("ABC1234");
    }

    [Fact]
    public async Task Handle_WithNonExistingVehicle_ShouldFail()
    {
        var vehicleId = Guid.CreateVersion7();
        _repository.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>()).Returns((Vehicle?)null);

        var result = await GetVehicleByIdHandler.Handle(new GetVehicleByIdQuery(vehicleId), _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
