namespace CatCar.Contexts.ServiceOperations.Tests.Domain;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using FluentAssertions;
using Xunit;

public class WorkOrderTests
{
    private static readonly Guid CustomerId = Guid.CreateVersion7();
    private static readonly Guid VehicleId = Guid.CreateVersion7();

    [Fact]
    public void Open_WithValidData_ShouldSucceedAsReceived()
    {
        var result = WorkOrder.Open(CustomerId, VehicleId, "Barulho estranho no motor.");

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be(CustomerId);
        result.Value.VehicleId.Should().Be(VehicleId);
        result.Value.Status.Should().Be(WorkOrderStatus.Received);
        result.Value.RequestedServices.Should().BeEmpty();
        result.Value.RequestedParts.Should().BeEmpty();
    }

    [Fact]
    public void Open_WithoutCustomer_ShouldFail()
    {
        var result = WorkOrder.Open(Guid.Empty, VehicleId, "Descrição válida");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Open_WithoutVehicle_ShouldFail()
    {
        var result = WorkOrder.Open(CustomerId, Guid.Empty, "Descrição válida");

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Open_WithoutInitialDescription_ShouldFail(string? description)
    {
        var result = WorkOrder.Open(CustomerId, VehicleId, description);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void StartDiagnosis_WhenReceived_ShouldTransitionToInDiagnosis()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida").Value;

        var result = workOrder.StartDiagnosis();

        result.IsSuccess.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.InDiagnosis);
    }

    [Fact]
    public void CompleteAndDeliver_WhenWorkOrderIsInExecution_ShouldTrackLifecycleTimestamps()
    {
        var workOrder = CreateWorkOrderAwaitingApproval();
        workOrder.RecordApproval(approved: true);

        var completeResult = workOrder.Complete();
        var deliverResult = workOrder.Deliver();

        completeResult.IsSuccess.Should().BeTrue();
        deliverResult.IsSuccess.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.Delivered);
        workOrder.BudgetApprovedAt.Should().NotBeNull();
        workOrder.CompletedAt.Should().NotBeNull();
        workOrder.DeliveredAt.Should().NotBeNull();
        workOrder.LastUpdatedAt.Should().Be(workOrder.DeliveredAt);
    }

    [Fact]
    public void StartDiagnosis_WhenNotReceived_ShouldFail()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida").Value;
        workOrder.StartDiagnosis();

        var result = workOrder.StartDiagnosis();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddRequestedService_WhenReceived_ShouldAddLine()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida").Value;

        var result = workOrder.AddRequestedService(Guid.CreateVersion7(), "Troca de óleo", 150m, 1);

        result.IsSuccess.Should().BeTrue();
        workOrder.RequestedServices.Should().HaveCount(1);
        workOrder.RequestedServices[0].LineTotal.Should().Be(150m);
    }

    [Fact]
    public void AddRequestedService_WithZeroQuantity_ShouldFail()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida").Value;

        var result = workOrder.AddRequestedService(Guid.CreateVersion7(), "Troca de óleo", 150m, 0);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddRequestedPart_WhenReceived_ShouldAddLine()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida").Value;

        var result = workOrder.AddRequestedPart(Guid.CreateVersion7(), "Filtro de óleo", 40m, 2);

        result.IsSuccess.Should().BeTrue();
        workOrder.RequestedParts.Should().HaveCount(1);
        workOrder.RequestedParts[0].LineTotal.Should().Be(80m);
    }

    [Fact]
    public void MarkBudgetIssued_WithoutLines_ShouldFail()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida").Value;

        var result = workOrder.MarkBudgetIssued(Guid.CreateVersion7());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarkBudgetIssued_WithLines_ShouldTransitionToAwaitingApproval()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida").Value;
        workOrder.AddRequestedService(Guid.CreateVersion7(), "Troca de óleo", 150m, 1);

        var budgetId = Guid.CreateVersion7();
        var result = workOrder.MarkBudgetIssued(budgetId);

        result.IsSuccess.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.AwaitingApproval);
        workOrder.ActiveBudgetId.Should().Be(budgetId);
    }

    [Fact]
    public void AddRequestedService_AfterBudgetIssued_ShouldFail()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida").Value;
        workOrder.AddRequestedService(Guid.CreateVersion7(), "Troca de óleo", 150m, 1);
        workOrder.MarkBudgetIssued(Guid.CreateVersion7());

        var result = workOrder.AddRequestedService(Guid.CreateVersion7(), "Alinhamento", 80m, 1);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecordApproval_WhenAwaitingApprovalAndApproved_ShouldTransitionToInExecution()
    {
        var workOrder = CreateWorkOrderAwaitingApproval();

        var result = workOrder.RecordApproval(approved: true);

        result.IsSuccess.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.InExecution);
    }

    [Fact]
    public void RecordApproval_WhenAwaitingApprovalAndRejected_ShouldReturnToDiagnosis()
    {
        var workOrder = CreateWorkOrderAwaitingApproval();

        var result = workOrder.RecordApproval(approved: false);

        result.IsSuccess.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.InDiagnosis);
        workOrder.ActiveBudgetId.Should().BeNull();
    }

    [Fact]
    public void RecordApproval_WhenNotAwaitingApproval_ShouldFail()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida").Value;

        var result = workOrder.RecordApproval(approved: true);

        result.IsFailure.Should().BeTrue();
    }

    private static WorkOrder CreateWorkOrderAwaitingApproval()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida").Value;
        workOrder.AddRequestedService(Guid.CreateVersion7(), "Troca de óleo", 150m, 1);
        workOrder.MarkBudgetIssued(Guid.CreateVersion7());
        return workOrder;
    }
}
