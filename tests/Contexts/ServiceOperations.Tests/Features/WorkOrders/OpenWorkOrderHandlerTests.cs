namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.OpenWorkOrder;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class OpenWorkOrderHandlerTests
{
    private readonly OpenWorkOrderCommandValidator _validator = new();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IVehicleRepository _vehicleRepository = Substitute.For<IVehicleRepository>();
    private readonly IWorkOrderRepository _workOrderRepository = Substitute.For<IWorkOrderRepository>();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldOpenWorkOrder()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;
        var vehicle = Vehicle.Register(customer.Id, "ABC1234", "Fiat", "Uno", 2015).Value;
        var command = new OpenWorkOrderCommand(customer.Id, vehicle.Id, "Barulho estranho no motor.");
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _vehicleRepository.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await OpenWorkOrderHandler.Handle(command, _validator, _customerRepository, _vehicleRepository, _workOrderRepository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(WorkOrderStatus.Received.ToString());
        await _workOrderRepository.Received(1).AddAsync(Arg.Any<WorkOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithVehicleFromAnotherCustomer_ShouldFail()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;
        var otherCustomerId = Guid.CreateVersion7();
        var vehicle = Vehicle.Register(otherCustomerId, "ABC1234", "Fiat", "Uno", 2015).Value;
        var command = new OpenWorkOrderCommand(customer.Id, vehicle.Id, "Barulho estranho no motor.");
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _vehicleRepository.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await OpenWorkOrderHandler.Handle(command, _validator, _customerRepository, _vehicleRepository, _workOrderRepository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _workOrderRepository.DidNotReceive().AddAsync(Arg.Any<WorkOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistingCustomer_ShouldFail()
    {
        var command = new OpenWorkOrderCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), "Barulho estranho no motor.");
        _customerRepository.GetByIdAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await OpenWorkOrderHandler.Handle(command, _validator, _customerRepository, _vehicleRepository, _workOrderRepository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistingVehicle_ShouldFail()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;
        var command = new OpenWorkOrderCommand(customer.Id, Guid.CreateVersion7(), "Barulho estranho no motor.");
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _vehicleRepository.GetByIdAsync(command.VehicleId, Arg.Any<CancellationToken>()).Returns((Vehicle?)null);

        var result = await OpenWorkOrderHandler.Handle(command, _validator, _customerRepository, _vehicleRepository, _workOrderRepository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
