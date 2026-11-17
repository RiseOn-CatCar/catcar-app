namespace CatCar.Contexts.CatalogInventory.Tests.Features.InventoryReservations;

using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using CatCar.Contexts.CatalogInventory.Features.InventoryReservations;
using CatCar.Contracts.CatalogInventory;
using NSubstitute;
using Xunit;

public class GetBudgetReservationOutcomesHandlerTests
{
    [Fact]
    public async Task Handle_MapsRepositoryOutcomesToPublishedResponses()
    {
        var repository = Substitute.For<IBudgetReservationOutcomeRepository>();
        var budgetId = Guid.NewGuid();
        var resolvedAt = DateTime.UtcNow;
        IReadOnlyList<Guid> budgetIds = [budgetId, Guid.NewGuid()];
        repository.GetByBudgetIdsAsync(budgetIds, Arg.Any<CancellationToken>()).Returns(
            [new BudgetReservationOutcome(budgetId, Guid.NewGuid(), ReservationOutcome.Failed,
                InventoryReservationFailureCode.InsufficientStock, resolvedAt, Guid.NewGuid())]);

        var results = await GetBudgetReservationOutcomesHandler.Handle(new GetBudgetReservationOutcomesQuery(budgetIds),
            repository, CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal(budgetId, result.BudgetId);
        Assert.Equal("Failed", result.Outcome);
        Assert.Equal("InsufficientStock", result.FailureCode);
        Assert.Equal(resolvedAt, result.ResolvedAt);
        await repository.Received(1).GetByBudgetIdsAsync(budgetIds, Arg.Any<CancellationToken>());
    }
}
