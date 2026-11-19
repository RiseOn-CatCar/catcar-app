namespace CatCar.Contexts.CatalogInventory.Tests.Integration;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contexts.CatalogInventory.Infrastructure;
using CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;
using CatCar.Contracts.CatalogInventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Xunit;

public sealed class InventoryOutboxRollbackTests : IntegrationTestBase
{
    [Fact]
    public async Task SaveChangesAndFlush_DuplicateOutcome_RollsBackStockReservationAndMessage()
    {
        var budgetId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var item = InventoryItem.Register("outbox-rollback", "Part", "Part", 1m, 10, 0).Value;
        await using (var seed = CreateDbContext())
        {
            seed.InventoryItems.Add(item);
            seed.BudgetReservationOutcomes.Add(new BudgetReservationOutcome(budgetId, workOrderId,
                ReservationOutcome.NoParts, null, DateTime.UtcNow, correlationId));
            await seed.SaveChangesAsync();
        }

        using var host = Host.CreateDefaultBuilder()
            .UseWolverine(options =>
            {
                options.PersistMessagesWithPostgresql(ConnectionString, schemaName: "wolverine");
                options.UseEntityFrameworkCoreTransactions();
                options.Policies.UseDurableLocalQueues();
                options.PublishMessage<InventoryReservedIntegrationEvent>().ToLocalQueue("inventory_rollback_test");
                options.UseRuntimeCompilation();
            })
            .ConfigureServices(services => services.AddDbContextWithWolverineIntegration<CatalogInventoryDbContext>(options =>
            {
                options.UseNpgsql(ConnectionString);
                options.AddInterceptors(new AuditInterceptor());
            }, wolverineDatabaseSchema: "wolverine"))
            .Build();

        await host.StartAsync();
        try
        {
            await using var scope = host.Services.CreateAsyncScope();
            var outbox = scope.ServiceProvider.GetRequiredService<IDbContextOutbox<CatalogInventoryDbContext>>();
            var repository = new InventoryItemRepository(outbox.DbContext);
            var reservation = InventoryReservation.Create(workOrderId, budgetId, item.Id, 3, DateTime.UtcNow).Value;
            Assert.Null(await repository.PrepareReservationAsync([reservation]));
            outbox.DbContext.BudgetReservationOutcomes.Add(new BudgetReservationOutcome(budgetId, workOrderId,
                ReservationOutcome.Reserved, null, DateTime.UtcNow, correlationId));
            var message = new InventoryReservedIntegrationEvent(workOrderId, budgetId,
                [new ReservedInventoryLine(item.Id, 3)], DateTime.UtcNow, correlationId);
            Assert.NotEmpty(outbox.PreviewSubscriptions(message));
            await outbox.PublishAsync(message);

            await Assert.ThrowsAsync<DbUpdateException>(() => outbox.SaveChangesAndFlushMessagesAsync());

            await using var verification = CreateDbContext();
            Assert.Equal(10, (await verification.InventoryItems.SingleAsync()).QuantityInStock);
            Assert.Empty(await verification.InventoryReservations.ToListAsync());
            Assert.Equal(ReservationOutcome.NoParts, (await verification.BudgetReservationOutcomes.SingleAsync()).Outcome);
            var connection = verification.Database.GetDbConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM wolverine.wolverine_incoming_envelopes";
            Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
