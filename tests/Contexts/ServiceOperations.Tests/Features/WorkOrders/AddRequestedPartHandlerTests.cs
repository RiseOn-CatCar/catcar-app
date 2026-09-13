namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedPart;
using CatCar.Contexts.ServiceOperations.Integrations;
using FluentAssertions;
using NSubstitute;
using RiseOn.RailResult.Upshot;
using Xunit;

public class AddRequestedPartHandlerTests
{
    private readonly AddRequestedPartCommandValidator _validator = new();
    private readonly IWorkOrderRepository _repository = Substitute.For<IWorkOrderRepository>();
    private readonly ICatalogInventoryAcl _acl = Substitute.For<ICatalogInventoryAcl>();

    [Fact]
    public async Task Handle_WithSufficientStock_ShouldAddLineWithoutReservingStock()
    {
        var workOrder = WorkOrder.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), "Descrição válida").Value;
        var inventoryItemId = Guid.CreateVersion7();
        var command = new AddRequestedPartCommand(workOrder.Id, inventoryItemId, 2);
        _repository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _acl.GetInventoryItemSnapshotAsync(inventoryItemId, Arg.Any<CancellationToken>())
            .Returns(Upshot<InventoryItemSnapshot>.Success(new InventoryItemSnapshot(inventoryItemId, "Filtro de óleo", 40m, true, 10)));

        var result = await AddRequestedPartHandler.Handle(command, _validator, _repository, _acl, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LineTotal.Should().Be(80m);
        workOrder.RequestedParts.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithInsufficientStock_ShouldFailAndNotAddLine()
    {
        var workOrder = WorkOrder.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), "Descrição válida").Value;
        var inventoryItemId = Guid.CreateVersion7();
        var command = new AddRequestedPartCommand(workOrder.Id, inventoryItemId, 5);
        _repository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _acl.GetInventoryItemSnapshotAsync(inventoryItemId, Arg.Any<CancellationToken>())
            .Returns(Upshot<InventoryItemSnapshot>.Success(new InventoryItemSnapshot(inventoryItemId, "Filtro de óleo", 40m, true, 2)));

        var result = await AddRequestedPartHandler.Handle(command, _validator, _repository, _acl, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        workOrder.RequestedParts.Should().BeEmpty();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistingWorkOrder_ShouldFail()
    {
        var command = new AddRequestedPartCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), 1);
        _repository.GetByIdAsync(command.WorkOrderId, Arg.Any<CancellationToken>()).Returns((WorkOrder?)null);

        var result = await AddRequestedPartHandler.Handle(command, _validator, _repository, _acl, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
