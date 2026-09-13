namespace CatCar.Contexts.ServiceOperations.Tests.Features.Vehicles;

using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.ListVehiclesByCustomer;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class ListVehiclesByCustomerHandlerTests
{
    private readonly IVehicleRepository _repository = Substitute.For<IVehicleRepository>();

    [Fact]
    public async Task Handle_ShouldReturnSummariesFromRepository()
    {
        var customerId = Guid.CreateVersion7();
        var vehicle = Vehicle.Register(customerId, "ABC1234", "Fiat", "Uno", 2015).Value;
        _repository.ListByCustomerAsync(customerId, null, Arg.Any<CancellationToken>()).Returns(new List<Vehicle> { vehicle });

        var result = await ListVehiclesByCustomerHandler.Handle(new ListVehiclesByCustomerQuery(customerId, null), _repository, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Plate.Should().Be("ABC1234");
    }
}
