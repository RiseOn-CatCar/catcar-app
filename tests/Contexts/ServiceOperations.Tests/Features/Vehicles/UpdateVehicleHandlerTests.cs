namespace CatCar.Contexts.ServiceOperations.Tests.Features.Vehicles;

using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.UpdateVehicle;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class UpdateVehicleHandlerTests
{
    private readonly UpdateVehicleCommandValidator _validator = new();
    private readonly IVehicleRepository _repository = Substitute.For<IVehicleRepository>();

    [Fact]
    public async Task Handle_WithExistingVehicle_ShouldUpdateDetails()
    {
        var vehicle = Vehicle.Register(Guid.CreateVersion7(), "ABC1234", "Fiat", "Uno", 2015).Value;
        var command = new UpdateVehicleCommand(vehicle.Id, "Fiat", "Uno Mille", 2016);
        _repository.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await UpdateVehicleHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Model.Should().Be("Uno Mille");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistingVehicle_ShouldFail()
    {
        var command = new UpdateVehicleCommand(Guid.CreateVersion7(), "Fiat", "Uno Mille", 2016);
        _repository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((Vehicle?)null);

        var result = await UpdateVehicleHandler.Handle(command, _validator, _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
