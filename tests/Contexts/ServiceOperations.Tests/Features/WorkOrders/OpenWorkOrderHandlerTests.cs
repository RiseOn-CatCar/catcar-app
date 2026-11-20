namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.OpenWorkOrder;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.Saga;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contexts.ServiceOperations.Integrations;
using Wolverine.EntityFrameworkCore;
using FluentAssertions;
using NSubstitute;
using RiseOn.RailResult.Upshot;
using Xunit;

public class OpenWorkOrderHandlerTests
{
    private readonly OpenWorkOrderCommandValidator _validator = new();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IVehicleRepository _vehicleRepository = Substitute.For<IVehicleRepository>();
    private readonly IWorkOrderRepository _workOrderRepository = Substitute.For<IWorkOrderRepository>();
    private readonly ICatalogInventoryAcl _catalogInventoryAcl = Substitute.For<ICatalogInventoryAcl>();
    private readonly IDbContextOutbox<ServiceOperationsDbContext> _outbox = Substitute.For<IDbContextOutbox<ServiceOperationsDbContext>>();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldOpenWorkOrder()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;
        var vehicle = Vehicle.Register(customer.Id, "ABC1234", "Fiat", "Uno", 2015).Value;
        var correlationId = Guid.CreateVersion7();
        var command = new OpenWorkOrderCommand(customer.Id, vehicle.Id, "Barulho estranho no motor.", CorrelationId: correlationId);
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _vehicleRepository.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await OpenWorkOrderHandler.Handle(command, _validator, _customerRepository, _vehicleRepository, _workOrderRepository, _catalogInventoryAcl, _outbox, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(WorkOrderStatus.Received.ToString());
        await _workOrderRepository.Received(1).AddAsync(Arg.Any<WorkOrder>(), Arg.Any<CancellationToken>());
        await _outbox.Received(1).PublishAsync(
            Arg.Is<WorkOrderStatusChanged>(@event => @event.CorrelationId == correlationId && @event.CustomerId == customer.Id && @event.CustomerName == customer.Name && @event.CustomerEmail == customer.Email));
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

        var result = await OpenWorkOrderHandler.Handle(command, _validator, _customerRepository, _vehicleRepository, _workOrderRepository, _catalogInventoryAcl, _outbox, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _workOrderRepository.DidNotReceive().AddAsync(Arg.Any<WorkOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistingCustomer_ShouldFail()
    {
        var command = new OpenWorkOrderCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), "Barulho estranho no motor.");
        _customerRepository.GetByIdAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await OpenWorkOrderHandler.Handle(command, _validator, _customerRepository, _vehicleRepository, _workOrderRepository, _catalogInventoryAcl, _outbox, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistingVehicle_ShouldFail()
    {
        var customer = Customer.Register("52998224725", "João da Silva", "(85) 99999-0000", null).Value;
        var command = new OpenWorkOrderCommand(customer.Id, Guid.CreateVersion7(), "Barulho estranho no motor.");
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _vehicleRepository.GetByIdAsync(command.VehicleId, Arg.Any<CancellationToken>()).Returns((Vehicle?)null);

        var result = await OpenWorkOrderHandler.Handle(command, _validator, _customerRepository, _vehicleRepository, _workOrderRepository, _catalogInventoryAcl, _outbox, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public async Task Handle_OpeningWithItemsWithoutValidEmail_OpensWithoutIssuingBudget(string? email)
    {
        var customer = Customer.Register("52998224725", "Customer", "123456", email).Value;
        var vehicle = Vehicle.Register(customer.Id, "ABC1234", "Fiat", "Uno", 2015).Value;
        var serviceId = Guid.NewGuid();
        var command = new OpenWorkOrderCommand(customer.Id, vehicle.Id, "Description",
            Services: [new OpenWorkOrderItemDto(serviceId, 1)]);
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _vehicleRepository.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);
        _catalogInventoryAcl.GetCatalogedServiceSnapshotAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Upshot<CatalogServiceSnapshot>.Success(new CatalogServiceSnapshot(serviceId, "Service", 100m, true)));

        var result = await OpenWorkOrderHandler.Handle(command, _validator, _customerRepository, _vehicleRepository,
            _workOrderRepository, _catalogInventoryAcl, _outbox, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(WorkOrderStatus.Received.ToString());
        await _workOrderRepository.Received(1).AddAsync(
            Arg.Is<WorkOrder>(order => order.RequestedServices.Count == 1), Arg.Any<CancellationToken>());
        await _outbox.Received(1).SaveChangesAndFlushMessagesAsync(Arg.Any<CancellationToken>());
    }
}
