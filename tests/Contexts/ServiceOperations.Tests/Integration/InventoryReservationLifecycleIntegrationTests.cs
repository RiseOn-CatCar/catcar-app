namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.InventoryReservations.InventoryReservationFailed;
using CatCar.Contexts.ServiceOperations.Features.InventoryReservations.InventoryReserved;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Xunit;

public sealed class InventoryReservationLifecycleIntegrationTests : IntegrationTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReservationResult_PersistsTransitionAndStatusOutboxMessage(bool reserved)
    {
        var connectionString = ConnectionString;
        using var host = Host.CreateDefaultBuilder()
            .UseWolverine(options =>
            {
                options.PersistMessagesWithPostgresql(connectionString, schemaName: "service_operations");
                options.UseEntityFrameworkCoreTransactions();
                options.Policies.UseDurableLocalQueues();
                options.PublishMessage<WorkOrderStatusChangedIntegrationEvent>().ToLocalQueue("status_changed");
                options.UseRuntimeCompilation();
            })
            .ConfigureServices(services => services.AddDbContextWithWolverineIntegration<ServiceOperationsDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
                options.AddInterceptors(new AuditInterceptor());
            }, wolverineDatabaseSchema: "service_operations"))
            .Build();
        await host.StartAsync();

        var now = DateTime.UtcNow;
        var order = WorkOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Description", now).Value;
        var service = Guid.NewGuid();
        order.AddRequestedService(service, "Service", 10m, 1);
        var budget = Budget.Issue(order.Id, [(BudgetLineType.Service, service, "Service", 10m, 1)], now).Value;
        order.MarkBudgetIssued(budget.Id, now);
        budget.Approve(now);
        order.RecordApproval(true, now);
        await using (var seedScope = host.Services.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<ServiceOperationsDbContext>();
            db.WorkOrders.Add(order);
            db.Budgets.Add(budget);
            await db.SaveChangesAsync();
        }

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var outbox = scope.ServiceProvider.GetRequiredService<IDbContextOutbox<ServiceOperationsDbContext>>();
            var orders = new WorkOrderRepository(outbox.DbContext);
            var budgets = new BudgetRepository(outbox.DbContext);
            if (reserved)
                await InventoryReservedHandler.Handle(new InventoryReservedIntegrationEvent(order.Id, budget.Id, [], now.AddMinutes(1), Guid.NewGuid()), orders, budgets, outbox, NullLogger<InventoryReservedLog>.Instance, CancellationToken.None);
            else
                await InventoryReservationFailedHandler.Handle(new InventoryReservationFailedIntegrationEvent(order.Id, budget.Id, "InsufficientStock", now.AddMinutes(1), Guid.NewGuid()), orders, budgets, outbox, NullLogger<InventoryReservationFailedLog>.Instance, CancellationToken.None);
        }

        await using (var verifyScope = host.Services.CreateAsyncScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<ServiceOperationsDbContext>();
            var persisted = await db.WorkOrders.SingleAsync(w => w.Id == order.Id);
            persisted.Status.Should().Be(reserved ? WorkOrderStatus.InExecution : WorkOrderStatus.InDiagnosis);
            persisted.ApprovedBudgetId.Should().Be(budget.Id);
            persisted.ActiveBudgetId.Should().Be(reserved ? budget.Id : null);
            persisted.InventoryReservationFailureCode.Should().Be(reserved ? null : "InsufficientStock");
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*)::int FROM service_operations.wolverine_incoming_envelopes";
            Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture).Should().BeGreaterThan(0);
        }
        await host.StopAsync();
    }
}
