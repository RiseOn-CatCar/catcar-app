namespace CatCar.Contexts.CatalogInventory.Tests.Integration;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contexts.CatalogInventory.Features.InventoryReservations.BudgetApproved;
using CatCar.Contexts.CatalogInventory.Features.InventoryReservations.WorkOrderStatusChanged;
using CatCar.Contexts.CatalogInventory.Infrastructure;
using CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RiseOn.RailResult.Upshot;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Xunit;

public sealed class InventoryReservationWorkflowTests : IntegrationTestBase
{
    [Fact]
    public async Task CompetingBudgets_ForLastItem_OnlyOneSucceeds()
    {
        var item = await AddItemAsync("last", 1);
        var first = Snapshot(Guid.NewGuid(), Guid.NewGuid(), Part(item, 1));
        var second = Snapshot(Guid.NewGuid(), Guid.NewGuid(), Part(item, 1));
        await WithHostAsync(async host =>
        {
            var gate = new ReservationGate();
            await Task.WhenAll(ApproveWithRetryAsync(host, first, gate), ApproveWithRetryAsync(host, second, gate));
        });
        await using var db = CreateDbContext();
        var outcomes = await db.BudgetReservationOutcomes.ToListAsync();
        Assert.Equal(2, outcomes.Count);
        Assert.Single(outcomes, x => x.Outcome == ReservationOutcome.Reserved);
        Assert.Single(outcomes, x => x.Outcome == ReservationOutcome.Failed && x.FailureCode == InventoryReservationFailureCode.InsufficientStock);
        Assert.Equal(0, (await db.InventoryItems.SingleAsync()).QuantityInStock);
        Assert.Single(await db.InventoryReservations.ToListAsync());
    }

    [Fact]
    public async Task MultipleParts_WhenOneIsInsufficient_NoStockIsDeducted()
    {
        var first = await AddItemAsync("first-part", 5);
        var second = await AddItemAsync("second-part", 1);
        var snapshot = Snapshot(Guid.NewGuid(), Guid.NewGuid(), Part(first, 2), Part(second, 2));
        await WithHostAsync(host => ApproveAsync(host, snapshot));
        await using var db = CreateDbContext();
        Assert.Equal(5, (await db.InventoryItems.SingleAsync(x => x.Id == first)).QuantityInStock);
        Assert.Equal(1, (await db.InventoryItems.SingleAsync(x => x.Id == second)).QuantityInStock);
        Assert.Empty(await db.InventoryReservations.ToListAsync());
        var outcome = await db.BudgetReservationOutcomes.SingleAsync();
        Assert.Equal(InventoryReservationFailureCode.InsufficientStock, outcome.FailureCode);
    }

    [Fact]
    public async Task NoParts_PersistsTerminalConfirmationWithoutReservations()
    {
        var snapshot = Snapshot(Guid.NewGuid(), Guid.NewGuid(), new BudgetSnapshotLine("Service", "Service", 1, 1, 1, Guid.NewGuid()));
        await WithHostAsync(host => ApproveAsync(host, snapshot));
        await using var db = CreateDbContext();
        Assert.Equal(ReservationOutcome.NoParts, (await db.BudgetReservationOutcomes.SingleAsync()).Outcome);
        Assert.Empty(await db.InventoryReservations.ToListAsync());
    }

    [Fact]
    public async Task SameBudgetConcurrentDeliveries_ReserveOnce()
    {
        var item = await AddItemAsync("same-budget", 3);
        var snapshot = Snapshot(Guid.NewGuid(), Guid.NewGuid(), Part(item, 1));
        await WithHostAsync(async host =>
        {
            var gate = new ReservationGate();
            await Task.WhenAll(ApproveWithRetryAsync(host, snapshot, gate), ApproveWithRetryAsync(host, snapshot, gate));
        });
        await using var db = CreateDbContext();
        Assert.Equal(2, (await db.InventoryItems.SingleAsync()).QuantityInStock);
        Assert.Single(await db.InventoryReservations.ToListAsync());
        Assert.Equal(ReservationOutcome.Reserved, (await db.BudgetReservationOutcomes.SingleAsync()).Outcome);
    }

    [Fact]
    public async Task CompletionAndApprovalReplay_DoNotDeductStockAgain()
    {
        var item = await AddItemAsync("completion", 4);
        var snapshot = Snapshot(Guid.NewGuid(), Guid.NewGuid(), Part(item, 2));
        await WithHostAsync(async host =>
        {
            await ApproveAsync(host, snapshot);
            await CompleteAsync(host, snapshot);
            await CompleteAsync(host, snapshot);
            await ApproveAsync(host, snapshot);
        });
        await using var db = CreateDbContext();
        Assert.Equal(2, (await db.InventoryItems.SingleAsync()).QuantityInStock);
        Assert.Equal(InventoryReservationStatus.Consumed, (await db.InventoryReservations.SingleAsync()).Status);
        Assert.Equal(ReservationOutcome.Reserved, (await db.BudgetReservationOutcomes.SingleAsync()).Outcome);
    }

    private async Task<Guid> AddItemAsync(string sku, int stock)
    {
        var item = InventoryItem.Register(sku, sku, sku, 1m, stock, 0).Value;
        await using var db = CreateDbContext();
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    private static BudgetSnapshotLine Part(Guid itemId, int quantity) => new("Part", "Part", 1, quantity, quantity, itemId);

    private static BudgetSnapshotResponse Snapshot(Guid budgetId, Guid workOrderId, params BudgetSnapshotLine[] lines)
        => new(true, budgetId, workOrderId, Guid.NewGuid(), "Customer", null, "Approved", 1, DateTime.UtcNow, lines);

    private static IMessageBus SnapshotBus(BudgetSnapshotResponse snapshot)
    {
        var bus = Substitute.For<IMessageBus>();
        bus.InvokeAsync<BudgetSnapshotResponse>(Arg.Any<GetBudgetSnapshotQuery>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        return bus;
    }

    private async Task WithHostAsync(Func<IHost, Task> action)
    {
        using var host = Host.CreateDefaultBuilder()
            .UseWolverine(options =>
            {
                options.PersistMessagesWithPostgresql(ConnectionString, schemaName: "wolverine");
                options.UseEntityFrameworkCoreTransactions();
                options.Policies.UseDurableLocalQueues();
                options.PublishMessage<InventoryReservedIntegrationEvent>().ToLocalQueue("inventory_reserved_test");
                options.PublishMessage<InventoryReservationFailedIntegrationEvent>().ToLocalQueue("inventory_failed_test");
                options.PublishMessage<InventoryConsumedIntegrationEvent>().ToLocalQueue("inventory_consumed_test");
                options.UseRuntimeCompilation();
            })
            .ConfigureServices(services => services.AddDbContextWithWolverineIntegration<CatalogInventoryDbContext>(options =>
            {
                options.UseNpgsql(ConnectionString);
                options.AddInterceptors(new AuditInterceptor());
            }, wolverineDatabaseSchema: "wolverine"))
            .Build();
        await host.StartAsync();
        try { await action(host); }
        finally { await host.StopAsync(); }
    }

    private static async Task ApproveWithRetryAsync(IHost host, BudgetSnapshotResponse snapshot, ReservationGate gate)
    {
        for (var attempt = 0; ; attempt++)
        {
            try { await ApproveAsync(host, snapshot, gate); return; }
            catch (Exception exception) when (attempt < 3 && exception is DbUpdateConcurrencyException or BudgetReservationOutcomeConflictException)
            {
                // A new scope and context mirror the handler retry policy.
            }
        }
    }

    private static async Task ApproveAsync(IHost host, BudgetSnapshotResponse snapshot, ReservationGate? gate = null)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IDbContextOutbox<CatalogInventoryDbContext>>();
        IInventoryItemRepository inventory = new InventoryItemRepository(outbox.DbContext);
        if (gate is not null) inventory = new GatedInventoryRepository(inventory, gate);
        await BudgetApprovedHandler.Handle(new BudgetApprovedIntegrationEvent(snapshot.BudgetId, snapshot.WorkOrderId,
            snapshot.CustomerId, snapshot.TotalAmount, DateTime.UtcNow, Guid.NewGuid()), SnapshotBus(snapshot), inventory,
            new InventoryReservationRepository(outbox.DbContext), new BudgetReservationOutcomeRepository(outbox.DbContext),
            outbox, NullLogger<BudgetApprovedHandler.Marker>.Instance, CancellationToken.None);
    }

    private static async Task CompleteAsync(IHost host, BudgetSnapshotResponse snapshot)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IDbContextOutbox<CatalogInventoryDbContext>>();
        var message = new WorkOrderStatusChangedIntegrationEvent(snapshot.WorkOrderId, snapshot.CustomerId,
            "InExecution", "Completed", DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), null, snapshot.BudgetId);
        await WorkOrderStatusChangedInventoryHandler.Handle(message, SnapshotBus(snapshot),
            new InventoryItemRepository(outbox.DbContext), new InventoryReservationRepository(outbox.DbContext),
            new BudgetReservationOutcomeRepository(outbox.DbContext), outbox, CancellationToken.None);
    }

    private sealed class ReservationGate
    {
        private int _arrivals;
        private readonly TaskCompletionSource _both = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task WaitAsync()
        {
            if (Interlocked.Increment(ref _arrivals) == 2) _both.TrySetResult();
            await _both.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
    }

    private sealed class GatedInventoryRepository(IInventoryItemRepository inner, ReservationGate gate) : IInventoryItemRepository
    {
        public Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => inner.GetByIdAsync(id, cancellationToken);
        public Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default) => inner.ExistsBySkuAsync(sku, cancellationToken);
        public Task<IReadOnlyList<InventoryItem>> ListAsync(bool? onlyActive, bool? onlyBelowMinimumStock, CancellationToken cancellationToken = default)
            => inner.ListAsync(onlyActive, onlyBelowMinimumStock, cancellationToken);
        public Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default) => inner.AddAsync(item, cancellationToken);
        public async Task<InventoryReservationFailureCode?> PrepareReservationAsync(IReadOnlyCollection<InventoryReservation> reservations, CancellationToken cancellationToken = default)
        {
            var result = await inner.PrepareReservationAsync(reservations, cancellationToken);
            if (result is null) await gate.WaitAsync();
            return result;
        }
        public Task<Upshot> PrepareReleaseAsync(IReadOnlyCollection<InventoryReservation> reservations, DateTime releasedAt, CancellationToken cancellationToken = default)
            => inner.PrepareReleaseAsync(reservations, releasedAt, cancellationToken);
        public Task<Upshot> PrepareConsumptionAsync(IReadOnlyCollection<InventoryReservation> reservations, DateTime consumedAt, CancellationToken cancellationToken = default)
            => inner.PrepareConsumptionAsync(reservations, consumedAt, cancellationToken);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => inner.SaveChangesAsync(cancellationToken);
    }
}
