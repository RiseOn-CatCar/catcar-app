namespace CatCar.Contexts.CatalogInventory.Tests.Integration;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class InventoryReservationPreparationTests : IntegrationTestBase
{
    [Fact]
    public async Task PrepareReservation_WhenAnyItemLacksStock_DoesNotMutateAnyStock()
    {
        await using var db = CreateDbContext();
        var first = InventoryItem.Register("first", "First", "First item", 1m, 10, 0).Value;
        var second = InventoryItem.Register("second", "Second", "Second item", 1m, 1, 0).Value;
        db.InventoryItems.AddRange(first, second);
        await db.SaveChangesAsync();
        var workOrder = Guid.NewGuid();
        var budget = Guid.NewGuid();
        var firstReservation = InventoryReservation.Create(workOrder, budget, first.Id, 3, DateTime.UtcNow).Value;
        var secondReservation = InventoryReservation.Create(workOrder, budget, second.Id, 2, DateTime.UtcNow).Value;
        var repository = new InventoryItemRepository(db);

        var failure = await repository.PrepareReservationAsync([firstReservation, secondReservation]);

        Assert.Equal(InventoryReservationFailureCode.InsufficientStock, failure);
        Assert.Equal(10, first.QuantityInStock);
        Assert.Equal(1, second.QuantityInStock);
        Assert.Empty(db.ChangeTracker.Entries<InventoryReservation>());
    }

    [Fact]
    public async Task PrepareReservation_DoesNotCommitBeforeCallerSaves()
    {
        await using var db = CreateDbContext();
        var item = InventoryItem.Register("staged", "Staged", "Staged item", 1m, 10, 0).Value;
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        var reservation = InventoryReservation.Create(Guid.NewGuid(), Guid.NewGuid(), item.Id, 3, DateTime.UtcNow).Value;
        var repository = new InventoryItemRepository(db);

        Assert.Null(await repository.PrepareReservationAsync([reservation]));
        await using var other = CreateDbContext();
        Assert.Equal(10, (await other.InventoryItems.SingleAsync()).QuantityInStock);
        Assert.Empty(await other.InventoryReservations.ToListAsync());
        await db.SaveChangesAsync();
        other.ChangeTracker.Clear();
        Assert.Equal(7, (await other.InventoryItems.SingleAsync()).QuantityInStock);
        Assert.Single(await other.InventoryReservations.ToListAsync());
    }

    [Fact]
    public async Task PrepareReservation_ConcurrentStockUpdate_RaisesConcurrencyException()
    {
        await using var seed = CreateDbContext();
        var item = InventoryItem.Register("race", "Race", "Race item", 1m, 5, 0).Value;
        seed.InventoryItems.Add(item);
        await seed.SaveChangesAsync();
        await using var first = CreateDbContext();
        await using var second = CreateDbContext();
        var firstRepository = new InventoryItemRepository(first);
        var secondRepository = new InventoryItemRepository(second);
        var firstReservation = InventoryReservation.Create(Guid.NewGuid(), Guid.NewGuid(), item.Id, 4, DateTime.UtcNow).Value;
        var secondReservation = InventoryReservation.Create(Guid.NewGuid(), Guid.NewGuid(), item.Id, 4, DateTime.UtcNow).Value;
        Assert.Null(await firstRepository.PrepareReservationAsync([firstReservation]));
        Assert.Null(await secondRepository.PrepareReservationAsync([secondReservation]));

        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }
}
