namespace CatCar.Contexts.ServiceOperations.Tests.Features.Vehicles;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.RegisterVehicle;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class RegisterVehicleHandlerTests
{
    private readonly RegisterVehicleCommandValidator _validator = new();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IVehicleRepository _vehicleRepository = Substitute.For<IVehicleRepository>();

    [Fact]
    public async Task Handle_WithValidCommandAndExistingCustomer_ShouldRegisterVehicle()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;
        var command = new RegisterVehicleCommand(customer.Id, "ABC1234", "Fiat", "Uno", 2015);
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _vehicleRepository.ExistsByPlateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await RegisterVehicleHandler.Handle(command, _validator, _customerRepository, _vehicleRepository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Brand.Should().Be("Fiat");
        await _vehicleRepository.Received(1).AddAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>());
        await _vehicleRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistingCustomer_ShouldFail()
    {
        var command = new RegisterVehicleCommand(Guid.CreateVersion7(), "ABC1234", "Fiat", "Uno", 2015);
        _customerRepository.GetByIdAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await RegisterVehicleHandler.Handle(command, _validator, _customerRepository, _vehicleRepository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _vehicleRepository.DidNotReceive().AddAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicatePlate_ShouldFailAndNotPersist()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;
        var command = new RegisterVehicleCommand(customer.Id, "ABC1234", "Fiat", "Uno", 2015);
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _vehicleRepository.ExistsByPlateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await RegisterVehicleHandler.Handle(command, _validator, _customerRepository, _vehicleRepository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _vehicleRepository.DidNotReceive().AddAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidCommand_ShouldFailValidationWithoutHittingRepositories()
    {
        var command = new RegisterVehicleCommand(Guid.Empty, "INVALID", string.Empty, "Uno", 2015);

        var result = await RegisterVehicleHandler.Handle(command, _validator, _customerRepository, _vehicleRepository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _customerRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
