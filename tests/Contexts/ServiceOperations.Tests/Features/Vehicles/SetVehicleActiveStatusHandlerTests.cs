namespace CatCar.Contexts.ServiceOperations.Tests.Features.Vehicles;

using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.SetVehicleActiveStatus;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class SetVehicleActiveStatusHandlerTests
{
    private readonly SetVehicleActiveStatusCommandValidator _validator = new();
    private readonly IVehicleRepository _repository = Substitute.For<IVehicleRepository>();

    [Fact]
    public async Task Handle_WithExistingVehicle_ShouldToggleActiveStatus()
    {
        var vehicle = Vehicle.Register(Guid.CreateVersion7(), "ABC1234", "Fiat", "Uno", 2015).Value;
        var command = new SetVehicleActiveStatusCommand(vehicle.Id, false);
        _repository.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await SetVehicleActiveStatusHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNonExistingVehicle_ShouldFail()
    {
        var command = new SetVehicleActiveStatusCommand(Guid.CreateVersion7(), false);
        _repository.GetByIdAsync(command.VehicleId, Arg.Any<CancellationToken>()).Returns((Vehicle?)null);

        var result = await SetVehicleActiveStatusHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
