namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedService;
using CatCar.Contexts.ServiceOperations.Integrations;
using FluentAssertions;
using NSubstitute;
using RiseOn.RailResult.Upshot;
using Xunit;

public class AddRequestedServiceHandlerTests
{
    private readonly AddRequestedServiceCommandValidator _validator = new();
    private readonly IWorkOrderRepository _repository = Substitute.For<IWorkOrderRepository>();
    private readonly ICatalogInventoryAcl _acl = Substitute.For<ICatalogInventoryAcl>();

    [Fact]
    public async Task Handle_WithValidCommandAndActiveService_ShouldAddLine()
    {
        var workOrder = WorkOrder.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), "Descrição válida").Value;
        var catalogedServiceId = Guid.CreateVersion7();
        var command = new AddRequestedServiceCommand(workOrder.Id, catalogedServiceId, 2);
        _repository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _acl.GetCatalogedServiceSnapshotAsync(catalogedServiceId, Arg.Any<CancellationToken>())
            .Returns(Upshot<CatalogServiceSnapshot>.Success(new CatalogServiceSnapshot(catalogedServiceId, "Troca de óleo", 150m, true)));

        var result = await AddRequestedServiceHandler.Handle(command, _validator, _repository, _acl, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LineTotal.Should().Be(300m);
        workOrder.RequestedServices.Should().HaveCount(1);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistingWorkOrder_ShouldFail()
    {
        var command = new AddRequestedServiceCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), 1);
        _repository.GetByIdAsync(command.WorkOrderId, Arg.Any<CancellationToken>()).Returns((WorkOrder?)null);

        var result = await AddRequestedServiceHandler.Handle(command, _validator, _repository, _acl, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithFailingAclSnapshot_ShouldFailAndNotPersist()
    {
        var workOrder = WorkOrder.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), "Descrição válida").Value;
        var catalogedServiceId = Guid.CreateVersion7();
        var command = new AddRequestedServiceCommand(workOrder.Id, catalogedServiceId, 1);
        _repository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _acl.GetCatalogedServiceSnapshotAsync(catalogedServiceId, Arg.Any<CancellationToken>())
            .Returns(Upshot<CatalogServiceSnapshot>.Fail("Serviço não encontrado no catálogo."));

        var result = await AddRequestedServiceHandler.Handle(command, _validator, _repository, _acl, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
