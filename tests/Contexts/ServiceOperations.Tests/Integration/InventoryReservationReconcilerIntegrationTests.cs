namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using CatCar.Api;
using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contexts.CatalogInventory.Features.InventoryReservations;
using CatCar.Contexts.CatalogInventory.Infrastructure;
using CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;
using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Xunit;

public sealed class InventoryReservationReconcilerIntegrationTests : IntegrationTestBase
{
    [Theory]
    [InlineData(ReservationOutcome.Reserved, null)]
    [InlineData(ReservationOutcome.NoParts, null)]
    [InlineData(ReservationOutcome.Failed, InventoryReservationFailureCode.InsufficientStock)]
    [InlineData(ReservationOutcome.Failed, InventoryReservationFailureCode.ReservationHistoryMismatch)]
    public async Task Reconcile_LegacyOrder_UsesDurableCatalogOutcome(ReservationOutcome outcome, InventoryReservationFailureCode? failure)
    {
        using var host = Host.CreateDefaultBuilder()
            .UseWolverine(options =>
            {
                options.Discovery.IncludeAssembly(typeof(GetBudgetReservationOutcomesHandler).Assembly);
                options.Discovery.IncludeAssembly(typeof(ServiceOperationsDbContext).Assembly);
                options.PersistMessagesWithPostgresql(ConnectionString, schemaName: "wolverine");
                options.UseEntityFrameworkCoreTransactions();
                options.Policies.UseDurableLocalQueues();
                options.UseRuntimeCompilation();
            })
            .ConfigureServices(services =>
            {
                services.AddDbContextWithWolverineIntegration<ServiceOperationsDbContext>(options =>
                {
                    options.UseNpgsql(ConnectionString);
                    options.AddInterceptors(new CatCar.Contexts.ServiceOperations.Infrastructure.Persistence.AuditInterceptor());
                }, wolverineDatabaseSchema: "wolverine");
                services.AddDbContextWithWolverineIntegration<CatalogInventoryDbContext>(options =>
                {
                    options.UseNpgsql(ConnectionString);
                    options.AddInterceptors(new CatCar.Contexts.CatalogInventory.Infrastructure.Persistence.AuditInterceptor());
                }, wolverineDatabaseSchema: "wolverine");
                services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
                services.AddScoped<IBudgetRepository, BudgetRepository>();
                services.AddScoped<ICustomerRepository, CustomerRepository>();
                services.AddScoped<IBudgetReservationOutcomeRepository, BudgetReservationOutcomeRepository>();
                services.AddScoped<IInventoryReservationRepository, InventoryReservationRepository>();
                services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
            })
            .Build();
        await host.StartAsync();
        try
        {
            var openedAt = DateTime.UtcNow.AddDays(-2);
            var order = WorkOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Legacy order", openedAt).Value;
            var item = InventoryItem.Register("legacy-part", "Part", "Part", 10m, 5, 0).Value;
            var isStockedReservation = outcome == ReservationOutcome.Reserved;
            if (isStockedReservation)
                order.AddRequestedPart(item.Id, "Part", 10m, 2);
            else
                order.AddRequestedService(Guid.NewGuid(), "Service", 10m, 1);
            var lines = isStockedReservation
                ? new List<(BudgetLineType, Guid, string, decimal, int)> { (BudgetLineType.Part, item.Id, "Part", 10m, 2) }
                : new List<(BudgetLineType, Guid, string, decimal, int)> { (BudgetLineType.Service, Guid.NewGuid(), "Service", 10m, 1) };
            var budget = Budget.Issue(order.Id, lines, openedAt).Value;
            order.MarkBudgetIssued(budget.Id, openedAt);
            budget.Approve(openedAt);
            order.RecordApproval(true, openedAt);
            order.ConfirmInventoryReservation(budget.Id, openedAt);
            typeof(WorkOrder).GetProperty(nameof(WorkOrder.HasCompleteStageHistory))!.SetValue(order, false);
            typeof(WorkOrder).GetProperty(nameof(WorkOrder.ExecutionStartedAt))!.SetValue(order, null);

            await using (var scope = host.Services.CreateAsyncScope())
            {
                var operations = scope.ServiceProvider.GetRequiredService<ServiceOperationsDbContext>();
                operations.WorkOrders.Add(order);
                operations.Budgets.Add(budget);
                await operations.SaveChangesAsync();
                var catalog = scope.ServiceProvider.GetRequiredService<CatalogInventoryDbContext>();
                await catalog.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
                if (isStockedReservation)
                    catalog.InventoryItems.Add(item);
                else
                    catalog.BudgetReservationOutcomes.Add(new BudgetReservationOutcome(budget.Id, order.Id, outcome, failure, DateTime.UtcNow, Guid.NewGuid()));
                await catalog.SaveChangesAsync();
            }

            if (failure == InventoryReservationFailureCode.ReservationHistoryMismatch)
            {
                var action = () => InventoryReservationReconciler.RunAsync(host.Services, CancellationToken.None);
                await action.Should().ThrowAsync<InvalidOperationException>();
            }
            else
            {
                await InventoryReservationReconciler.RunAsync(host.Services, CancellationToken.None);
            }

            await using var verification = host.Services.CreateAsyncScope();
            var db = verification.ServiceProvider.GetRequiredService<ServiceOperationsDbContext>();
            var persisted = await db.WorkOrders.SingleAsync(x => x.Id == order.Id);
            persisted.Status.Should().Be(failure == InventoryReservationFailureCode.InsufficientStock ? WorkOrderStatus.InDiagnosis : WorkOrderStatus.InExecution);
            persisted.ApprovedBudgetId.Should().Be(budget.Id);
            persisted.ActiveBudgetId.Should().Be(failure == InventoryReservationFailureCode.InsufficientStock ? null : budget.Id);
            persisted.InventoryReservationFailureCode.Should().Be(failure == InventoryReservationFailureCode.InsufficientStock ? "InsufficientStock" : null);
            if (isStockedReservation)
            {
                var catalog = verification.ServiceProvider.GetRequiredService<CatalogInventoryDbContext>();
                var storedItem = await catalog.InventoryItems.SingleAsync(x => x.Id == item.Id);
                var reservation = await catalog.InventoryReservations.SingleAsync(x => x.BudgetId == budget.Id);
                var storedOutcome = await catalog.BudgetReservationOutcomes.SingleAsync(x => x.BudgetId == budget.Id);
                storedItem.QuantityInStock.Should().Be(3);
                reservation.Quantity.Should().Be(2);
                reservation.Status.Should().Be(InventoryReservationStatus.Reserved);
                storedOutcome.Outcome.Should().Be(ReservationOutcome.Reserved);
            }
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
