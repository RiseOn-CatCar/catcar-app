namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.ListWorkOrders;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class ListWorkOrdersHandlerTests
{
    private readonly IWorkOrderRepository _repository = Substitute.For<IWorkOrderRepository>();

    [Fact]
    public async Task Handle_WithoutFilters_ShouldReturnAllSummaries()
    {
        var workOrder = WorkOrder.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), "Descrição válida").Value;
        _repository.ListAsync(null, null, false, Arg.Any<CancellationToken>()).Returns(new List<WorkOrder> { workOrder });

        var result = await ListWorkOrdersHandler.Handle(new ListWorkOrdersQuery(null, null), _repository, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithValidStatusFilter_ShouldForwardParsedStatusToRepository()
    {
        _repository.ListAsync(Arg.Any<Guid?>(), WorkOrderStatus.Received, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(new List<WorkOrder>());

        await ListWorkOrdersHandler.Handle(new ListWorkOrdersQuery(null, "Received"), _repository, CancellationToken.None);

        await _repository.Received(1).ListAsync(Arg.Any<Guid?>(), WorkOrderStatus.Received, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidStatusFilter_ShouldFailWithoutHittingRepository()
    {
        var result = await ListWorkOrdersHandler.Handle(new ListWorkOrdersQuery(null, "NotAStatus"), _repository, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().ListAsync(Arg.Any<Guid?>(), Arg.Any<WorkOrderStatus?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithCustomerIdFilter_ShouldForwardToRepository()
    {
        var customerId = Guid.CreateVersion7();
        _repository.ListAsync(customerId, null, false, Arg.Any<CancellationToken>()).Returns(new List<WorkOrder>());

        await ListWorkOrdersHandler.Handle(new ListWorkOrdersQuery(customerId, null), _repository, CancellationToken.None);

        await _repository.Received(1).ListAsync(customerId, null, false, Arg.Any<CancellationToken>());
    }
}
