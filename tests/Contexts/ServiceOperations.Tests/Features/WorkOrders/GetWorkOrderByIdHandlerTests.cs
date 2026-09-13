namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderById;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class GetWorkOrderByIdHandlerTests
{
    private readonly IWorkOrderRepository _repository = Substitute.For<IWorkOrderRepository>();

    [Fact]
    public async Task Handle_WithExistingWorkOrder_ShouldReturnDetailsWithLines()
    {
        var workOrder = WorkOrder.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), "Descrição válida").Value;
        workOrder.AddRequestedService(Guid.CreateVersion7(), "Troca de óleo", 150m, 1);
        workOrder.AddRequestedPart(Guid.CreateVersion7(), "Filtro de óleo", 40m, 2);
        _repository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var result = await GetWorkOrderByIdHandler.Handle(new GetWorkOrderByIdQuery(workOrder.Id), _repository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestedServices.Should().HaveCount(1);
        result.Value.RequestedParts.Should().HaveCount(1);
        result.Value.Status.Should().Be(WorkOrderStatus.Received.ToString());
    }

    [Fact]
    public async Task Handle_WithNonExistingWorkOrder_ShouldFail()
    {
        var workOrderId = Guid.CreateVersion7();
        _repository.GetByIdAsync(workOrderId, Arg.Any<CancellationToken>()).Returns((WorkOrder?)null);

        var result = await GetWorkOrderByIdHandler.Handle(new GetWorkOrderByIdQuery(workOrderId), _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
