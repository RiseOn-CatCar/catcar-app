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
        var result = WorkOrder.Open(CustomerId, VehicleId, "Barulho estranho no motor.", DateTime.UtcNow);

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
        var result = WorkOrder.Open(Guid.Empty, VehicleId, "Descrição válida", DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Open_WithoutVehicle_ShouldFail()
    {
        var result = WorkOrder.Open(CustomerId, Guid.Empty, "Descrição válida", DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Open_WithoutInitialDescription_ShouldFail(string? description)
    {
        var result = WorkOrder.Open(CustomerId, VehicleId, description, DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void StartDiagnosis_WhenReceived_ShouldTransitionToInDiagnosis()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida", DateTime.UtcNow).Value;

        var result = workOrder.StartDiagnosis(DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.InDiagnosis);
    }

    [Fact]
    public void CompleteAndDeliver_WhenWorkOrderIsInExecution_ShouldTrackLifecycleTimestamps()
    {
        var workOrder = CreateWorkOrderAwaitingApproval();
        workOrder.RecordApproval(approved: true, DateTime.UtcNow);
        workOrder.ConfirmInventoryReservation(workOrder.ActiveBudgetId!.Value, DateTime.UtcNow);
        workOrder.StartExecution(DateTime.UtcNow);

        var completeResult = workOrder.Complete(DateTime.UtcNow);
        var deliverResult = workOrder.Deliver(DateTime.UtcNow);

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
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida", DateTime.UtcNow).Value;
        workOrder.StartDiagnosis(DateTime.UtcNow);

        var result = workOrder.StartDiagnosis(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddRequestedService_WhenReceived_ShouldAddLine()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida", DateTime.UtcNow).Value;

        var result = workOrder.AddRequestedService(Guid.CreateVersion7(), "Troca de óleo", 150m, 1);

        result.IsSuccess.Should().BeTrue();
        workOrder.RequestedServices.Should().HaveCount(1);
        workOrder.RequestedServices[0].LineTotal.Should().Be(150m);
    }

    [Fact]
    public void AddRequestedService_WithZeroQuantity_ShouldFail()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida", DateTime.UtcNow).Value;

        var result = workOrder.AddRequestedService(Guid.CreateVersion7(), "Troca de óleo", 150m, 0);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddRequestedPart_WhenReceived_ShouldAddLine()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida", DateTime.UtcNow).Value;

        var result = workOrder.AddRequestedPart(Guid.CreateVersion7(), "Filtro de óleo", 40m, 2);

        result.IsSuccess.Should().BeTrue();
        workOrder.RequestedParts.Should().HaveCount(1);
        workOrder.RequestedParts[0].LineTotal.Should().Be(80m);
    }

    [Fact]
    public void MarkBudgetIssued_WithoutLines_ShouldFail()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida", DateTime.UtcNow).Value;
        workOrder.StartDiagnosis(DateTime.UtcNow);

        var result = workOrder.MarkBudgetIssued(Guid.CreateVersion7(), DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarkBudgetIssued_WithLines_ShouldTransitionToAwaitingApproval()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida", DateTime.UtcNow).Value;
        workOrder.AddRequestedService(Guid.CreateVersion7(), "Troca de óleo", 150m, 1);
        workOrder.StartDiagnosis(DateTime.UtcNow);

        var budgetId = Guid.CreateVersion7();
        var result = workOrder.MarkBudgetIssued(budgetId, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.AwaitingApproval);
        workOrder.ActiveBudgetId.Should().Be(budgetId);
    }

    [Fact]
    public void AddRequestedService_AfterBudgetIssued_ShouldFail()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida", DateTime.UtcNow).Value;
        workOrder.AddRequestedService(Guid.CreateVersion7(), "Troca de óleo", 150m, 1);
        workOrder.StartDiagnosis(DateTime.UtcNow);
        workOrder.MarkBudgetIssued(Guid.CreateVersion7(), DateTime.UtcNow);

        var result = workOrder.AddRequestedService(Guid.CreateVersion7(), "Alinhamento", 80m, 1);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecordApproval_WhenAwaitingApprovalAndApproved_ShouldRemainAwaitingReservation()
    {
        var workOrder = CreateWorkOrderAwaitingApproval();

        var result = workOrder.RecordApproval(approved: true, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.AwaitingApproval);
    }

    [Fact]
    public void RecordApproval_WhenAwaitingApprovalAndRejected_ShouldReturnToDiagnosis()
    {
        var workOrder = CreateWorkOrderAwaitingApproval();

        var result = workOrder.RecordApproval(approved: false, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.InDiagnosis);
        workOrder.ActiveBudgetId.Should().BeNull();
    }

    [Fact]
    public void RecordApproval_WhenNotAwaitingApproval_ShouldFail()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida", DateTime.UtcNow).Value;

        var result = workOrder.RecordApproval(approved: true, DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ReservationFailure_RequiresNewBudgetAndPreservesApprovalHistory()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var order = WorkOrder.Open(CustomerId, VehicleId, "Description", start).Value;
        order.AddRequestedService(Guid.NewGuid(), "Service", 10m, 1);
        order.StartDiagnosis(start.AddHours(1));
        var firstBudget = Guid.NewGuid();
        order.MarkBudgetIssued(firstBudget, start.AddHours(2));
        order.RecordApproval(true, start.AddHours(3));
        order.ConfirmInventoryReservation(Guid.NewGuid(), start.AddHours(4)).IsFailure.Should().BeTrue();
        order.RecordInventoryReservationFailure(firstBudget, "InsufficientStock", start.AddHours(4)).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(WorkOrderStatus.InDiagnosis);
        order.ApprovedBudgetId.Should().Be(firstBudget);
        order.BudgetApprovedAt.Should().Be(start.AddHours(3));
        order.ActiveBudgetId.Should().BeNull();
        var secondBudget = Guid.NewGuid();
        order.MarkBudgetIssued(secondBudget, start.AddHours(5));
        order.InventoryReservationFailedAt.Should().BeNull();
        order.InventoryReservationFailureCode.Should().BeNull();
        order.RecordApproval(true, start.AddHours(6));
        order.ConfirmInventoryReservation(secondBudget, start.AddHours(7)).IsSuccess.Should().BeTrue();
        order.StartExecution(start.AddHours(7));
        order.Complete(start.AddHours(9));
        order.Deliver(start.AddHours(10));
        order.DiagnosisDurationSeconds.Should().Be(7200);
        order.ExecutionDurationSeconds.Should().Be(7200);
        order.FinalizationDurationSeconds.Should().Be(3600);
        order.ExecutionStartedAt.Should().Be(start.AddHours(7));
    }

    [Fact]
    public void CompleteLifecycle_TracksStageDurationsWithoutApprovalWait()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var order = WorkOrder.Open(CustomerId, VehicleId, "Description", start).Value;
        var budget = Guid.NewGuid();
        order.AddRequestedService(Guid.NewGuid(), "Service", 10m, 1);
        order.StartDiagnosis(start.AddHours(1));
        order.MarkBudgetIssued(budget, start.AddHours(3));
        order.RecordApproval(true, start.AddHours(6));
        order.ConfirmInventoryReservation(budget, start.AddHours(8));
        order.StartExecution(start.AddHours(8));
        order.Complete(start.AddHours(11));
        order.Deliver(start.AddHours(12));

        order.DiagnosisDurationSeconds.Should().Be(7200);
        order.ExecutionDurationSeconds.Should().Be(10800);
        order.FinalizationDurationSeconds.Should().Be(3600);
    }

    [Fact]
    public void LegacyHistory_RemainsUnknownAfterLaterTransitions()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var order = WorkOrder.Open(CustomerId, VehicleId, "Description", start).Value;
        typeof(WorkOrder).GetProperty(nameof(WorkOrder.HasCompleteStageHistory))!.SetValue(order, false);
        var budget = Guid.NewGuid();
        order.AddRequestedService(Guid.NewGuid(), "Service", 10m, 1);
        order.StartDiagnosis(start.AddHours(1));
        order.MarkBudgetIssued(budget, start.AddHours(3));
        order.RecordApproval(true, start.AddHours(4));
        order.ConfirmInventoryReservation(budget, start.AddHours(5));
        order.StartExecution(start.AddHours(5));
        order.Complete(start.AddHours(8));
        order.Deliver(start.AddHours(9));

        order.StatusEnteredAt.Should().Be(start.AddHours(9));
        order.DiagnosisDurationSeconds.Should().BeNull();
        order.ExecutionDurationSeconds.Should().BeNull();
        order.FinalizationDurationSeconds.Should().BeNull();
    }

    [Fact]
    public void BackdatedTransitions_LeaveOrderUnchanged()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var order = WorkOrder.Open(CustomerId, VehicleId, "Description", start).Value;
        var budget = Guid.NewGuid();
        order.AddRequestedService(Guid.NewGuid(), "Service", 10m, 1);
        order.StartDiagnosis(start.AddHours(1));
        order.MarkBudgetIssued(budget, start).IsFailure.Should().BeTrue();
        order.Status.Should().Be(WorkOrderStatus.InDiagnosis);
        order.DiagnosisDurationSeconds.Should().BeNull();
        order.MarkBudgetIssued(budget, start.AddHours(2));
        order.RecordApproval(true, start.AddHours(1)).IsFailure.Should().BeTrue();
        order.ApprovedBudgetId.Should().BeNull();
        order.RecordApproval(true, start.AddHours(3));
        order.ConfirmInventoryReservation(budget, start.AddHours(2)).IsFailure.Should().BeTrue();
        order.RecordInventoryReservationFailure(budget, "InsufficientStock", start.AddHours(2)).IsFailure.Should().BeTrue();
        order.Status.Should().Be(WorkOrderStatus.AwaitingApproval);
        order.ActiveBudgetId.Should().Be(budget);
        order.ConfirmInventoryReservation(budget, start.AddHours(4));
        order.StartExecution(start.AddHours(4));
        order.Complete(start.AddHours(3)).IsFailure.Should().BeTrue();
        order.ExecutionDurationSeconds.Should().BeNull();
        order.Complete(start.AddHours(5));
        order.Deliver(start.AddHours(4)).IsFailure.Should().BeTrue();
        order.Status.Should().Be(WorkOrderStatus.Completed);
        order.FinalizationDurationSeconds.Should().BeNull();
    }

    [Fact]
    public void ReconcileFailure_WhenExecutionStartIsKnown_DoesNotChangeOrder()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var order = WorkOrder.Open(CustomerId, VehicleId, "Description", start).Value;
        var budget = Guid.NewGuid();
        order.AddRequestedService(Guid.NewGuid(), "Service", 10m, 1);
        order.StartDiagnosis(start);
        order.MarkBudgetIssued(budget, start);
        order.RecordApproval(true, start);
        order.ConfirmInventoryReservation(budget, start);
        order.StartExecution(start);
        typeof(WorkOrder).GetProperty(nameof(WorkOrder.HasCompleteStageHistory))!.SetValue(order, false);

        order.ReconcileInventoryReservationFailure(budget, "InsufficientStock", start.AddHours(1)).IsFailure.Should().BeTrue();
        order.Status.Should().Be(WorkOrderStatus.InExecution);
        order.ActiveBudgetId.Should().Be(budget);
        order.InventoryReservationFailedAt.Should().BeNull();
    }

    [Fact]
    public void PaymentAndCancellation_EnforceLifecycleAndTimestamp()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var order = WorkOrder.Open(CustomerId, VehicleId, "Description", start).Value;
        order.AddRequestedService(Guid.NewGuid(), "Service", 10m, 1);
        order.MarkBudgetIssued(Guid.NewGuid(), start).IsFailure.Should().BeTrue();
        order.StartDiagnosis(start);
        order.MarkBudgetIssued(Guid.Empty, start).IsFailure.Should().BeTrue();
        order.MarkBudgetIssued(Guid.NewGuid(), start).IsSuccess.Should().BeTrue();
        order.AwaitPayment(start).IsFailure.Should().BeTrue();
        order.StartExecution(start).IsFailure.Should().BeTrue();
        order.RecordApproval(true, start).IsSuccess.Should().BeTrue();
        order.AwaitPayment(start.AddHours(1)).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(WorkOrderStatus.AwaitingPayment);
        order.StartExecution(start).IsFailure.Should().BeTrue();
        order.Cancel(start.AddHours(2)).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(WorkOrderStatus.Cancelled);
        order.Cancel(start.AddHours(3)).IsFailure.Should().BeTrue();
        order.StartExecution(start.AddHours(3)).IsFailure.Should().BeTrue();
    }

    private static WorkOrder CreateWorkOrderAwaitingApproval()
    {
        var workOrder = WorkOrder.Open(CustomerId, VehicleId, "Descrição válida", DateTime.UtcNow).Value;
        workOrder.AddRequestedService(Guid.CreateVersion7(), "Troca de óleo", 150m, 1);
        workOrder.StartDiagnosis(DateTime.UtcNow);
        workOrder.MarkBudgetIssued(Guid.CreateVersion7(), DateTime.UtcNow);
        return workOrder;
    }
}
