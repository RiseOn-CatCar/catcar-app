namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class WorkOrderRepositoryIntegrationTests : IntegrationTestBase
{
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
                 last_updated_at, created_at, created_by, updated_at, updated_by)
            VALUES
                ({firstWorkOrderId}, {customerId}, {firstVehicleId}, {"First order"}, {"Completed"}, {activeBudgetId},
                 {firstOpenedAt}, {firstOpenedAt.AddHours(2)}, {firstOpenedAt.AddHours(5)}, {firstOpenedAt.AddHours(9)}, {null},
                 {firstOpenedAt.AddHours(9)}, {firstOpenedAt}, {"integration-test"}, {firstOpenedAt}, {"integration-test"})
            """);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO service_operations.work_orders
                (id, customer_id, vehicle_id, initial_description, status, active_budget_id,
                 opened_at, diagnosis_started_at, budget_approved_at, completed_at, delivered_at,
                 last_updated_at, created_at, created_by, updated_at, updated_by)
            VALUES
                ({secondWorkOrderId}, {customerId}, {secondVehicleId}, {"Second order"}, {"Delivered"}, {activeBudgetId},
                 {secondOpenedAt}, {secondOpenedAt.AddHours(2)}, {secondOpenedAt.AddHours(5)}, {secondOpenedAt.AddHours(9)}, {secondOpenedAt.AddHours(10)},
                 {secondOpenedAt.AddHours(10)}, {secondOpenedAt}, {"integration-test"}, {secondOpenedAt}, {"integration-test"})
            """);

        var repository = new WorkOrderRepository(context);

        var metrics = await repository.GetAverageExecutionTimeAsync();

        metrics.TotalCompletedWorkOrders.Should().Be(2);
        metrics.AverageTotalExecutionTimeHours.Should().BeApproximately(9, 0.01);
        metrics.AverageDiagnosisTimeHours.Should().BeApproximately(3, 0.01);
        metrics.AverageExecutionTimeHours.Should().BeApproximately(4, 0.01);
        metrics.AverageFinalizationTimeHours.Should().BeApproximately(1, 0.01);
    }
}
