namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderProgress;
using FluentAssertions;
using NSubstitute;
using Xunit;

public sealed class GetWorkOrderProgressHandlerTests
{
    [Fact]
    public async Task Handle_WhenWorkOrderBelongsToAnotherCustomer_ReturnsFailure()
    {
        var ownerId = Guid.CreateVersion7();
        var workOrder = WorkOrder.Open(ownerId, Guid.CreateVersion7(), "Valid description").Value;
        var repository = Substitute.For<IWorkOrderRepository>();
        repository.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var result = await GetWorkOrderProgressHandler.Handle(
            new GetWorkOrderProgressQuery(workOrder.Id, Guid.CreateVersion7()),
            repository,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
