namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class WorkOrderRepositoryIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task GetAverageExecutionTimeAsync_WithNoOrders_ReturnsUnknownStages()
    {
        await using var context = CreateDbContext();
        var metrics = await new WorkOrderRepository(context).GetAverageExecutionTimeAsync();
        metrics.TotalCompletedWorkOrders.Should().Be(0);
        metrics.AverageDiagnosisTimeHours.Should().BeNull();
        metrics.AverageExecutionTimeHours.Should().BeNull();
        metrics.AverageFinalizationTimeHours.Should().BeNull();
    }

    [Fact]
    public async Task WorkOrder_PaymentAndExecutionTransitions_PersistAcrossContexts()
    {
        var now = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var order = WorkOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Description", now).Value;
        var budgetId = Guid.NewGuid();
        order.AddRequestedService(Guid.NewGuid(), "Service", 100m, 1).IsSuccess.Should().BeTrue();
        order.StartDiagnosis(now.AddHours(1)).IsSuccess.Should().BeTrue();
        order.MarkBudgetIssued(budgetId, now.AddHours(2)).IsSuccess.Should().BeTrue();
        order.RecordApproval(true, now.AddHours(3)).IsSuccess.Should().BeTrue();
        order.AwaitPayment(now.AddHours(4)).IsSuccess.Should().BeTrue();

        await using (var context = CreateDbContext())
        {
            await new WorkOrderRepository(context).AddAsync(order);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateDbContext())
        {
            var persisted = await new WorkOrderRepository(context).GetByIdAsync(order.Id);
            persisted.Should().NotBeNull();
            persisted!.Status.Should().Be(WorkOrderStatus.AwaitingPayment);
            persisted.ActiveBudgetId.Should().Be(budgetId);
            persisted.ApprovedBudgetId.Should().Be(budgetId);
            persisted.RequestedServices.Should().ContainSingle();
            persisted.StartExecution(now.AddHours(5)).IsSuccess.Should().BeTrue();
            persisted.Complete(now.AddHours(7)).IsSuccess.Should().BeTrue();
            await context.SaveChangesAsync();
        }

        await using (var context = CreateDbContext())
        {
            var persisted = await new WorkOrderRepository(context).GetByIdAsync(order.Id);
            persisted.Should().NotBeNull();
            persisted!.Status.Should().Be(WorkOrderStatus.Completed);
            persisted.ExecutionStartedAt.Should().Be(now.AddHours(5));
            persisted.ExecutionDurationSeconds.Should().Be(7200);
        }
    }

    [Fact]
    public async Task ListAsync_WhenCancelled_ExcludesOrderUnlessClosedOrdersRequested()
    {
        var now = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var order = WorkOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Description", now).Value;
        order.AddRequestedService(Guid.NewGuid(), "Service", 100m, 1).IsSuccess.Should().BeTrue();
        order.StartDiagnosis(now).IsSuccess.Should().BeTrue();
        order.MarkBudgetIssued(Guid.NewGuid(), now).IsSuccess.Should().BeTrue();
        order.Cancel(now).IsSuccess.Should().BeTrue();

        await using (var context = CreateDbContext())
        {
            await new WorkOrderRepository(context).AddAsync(order);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateDbContext())
        {
            var repository = new WorkOrderRepository(context);
            var open = await repository.ListAsync(null, null, false);
            var all = await repository.ListAsync(null, null, true);

            open.Should().NotContain(w => w.Id == order.Id);
            all.Should().ContainSingle(w => w.Id == order.Id && w.Status == WorkOrderStatus.Cancelled);
        }
    }

    [Fact]
    public async Task GetAverageExecutionTimeAsync_WithCompletedWorkOrders_ShouldAggregateInPostgres()
    {
        var customerId = Guid.CreateVersion7();
        var firstWorkOrderId = Guid.CreateVersion7();
        var secondWorkOrderId = Guid.CreateVersion7();
        var firstVehicleId = Guid.CreateVersion7();
        var secondVehicleId = Guid.CreateVersion7();
        var firstOpenedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var secondOpenedAt = new DateTime(2026, 1, 2, 8, 0, 0, DateTimeKind.Utc);
        Guid? activeBudgetId = null;

        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO service_operations.work_orders
                (id, customer_id, vehicle_id, initial_description, status, active_budget_id,
                 opened_at, diagnosis_started_at, budget_approved_at, completed_at, delivered_at,
                 last_updated_at, created_at, created_by, updated_at, updated_by, has_complete_stage_history)
            VALUES
                ({firstWorkOrderId}, {customerId}, {firstVehicleId}, {"First order"}, {"Completed"}, {activeBudgetId},
                 {firstOpenedAt}, {firstOpenedAt.AddHours(2)}, {firstOpenedAt.AddHours(5)}, {firstOpenedAt.AddHours(9)}, {null},
                 {firstOpenedAt.AddHours(9)}, {firstOpenedAt}, {"integration-test"}, {firstOpenedAt}, {"integration-test"}, {false})
            """);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO service_operations.work_orders
                (id, customer_id, vehicle_id, initial_description, status, active_budget_id,
                 opened_at, diagnosis_started_at, budget_approved_at, completed_at, delivered_at,
                 last_updated_at, created_at, created_by, updated_at, updated_by, has_complete_stage_history)
            VALUES
                ({secondWorkOrderId}, {customerId}, {secondVehicleId}, {"Second order"}, {"Delivered"}, {activeBudgetId},
                 {secondOpenedAt}, {secondOpenedAt.AddHours(2)}, {secondOpenedAt.AddHours(5)}, {secondOpenedAt.AddHours(9)}, {secondOpenedAt.AddHours(10)},
                 {secondOpenedAt.AddHours(10)}, {secondOpenedAt}, {"integration-test"}, {secondOpenedAt}, {"integration-test"}, {false})
            """);

        var repository = new WorkOrderRepository(context);

        var metrics = await repository.GetAverageExecutionTimeAsync();

        metrics.TotalCompletedWorkOrders.Should().Be(2);
        metrics.AverageTotalExecutionTimeHours.Should().BeApproximately(9, 0.01);
        metrics.AverageDiagnosisTimeHours.Should().BeNull();
        metrics.AverageExecutionTimeHours.Should().BeNull();
        metrics.AverageFinalizationTimeHours.Should().BeNull();
    }
}
