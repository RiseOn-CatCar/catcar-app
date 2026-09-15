namespace CatCar.Contexts.CatalogInventory.Tests.Domain;

using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using FluentAssertions;
using Xunit;

public class InventoryReservationTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldSucceedWithReservedStatus()
    {
        var workOrderId = Guid.CreateVersion7();
        var budgetId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        var result = InventoryReservation.Create(workOrderId, budgetId, itemId, 5, now);

        result.IsSuccess.Should().BeTrue();
        var reservation = result.Value;
        reservation.WorkOrderId.Should().Be(workOrderId);
        reservation.BudgetId.Should().Be(budgetId);
        reservation.InventoryItemId.Should().Be(itemId);
        reservation.Quantity.Should().Be(5);
        reservation.Status.Should().Be(InventoryReservationStatus.Reserved);
        reservation.ReservedAt.Should().Be(now);
        reservation.ConsumedAt.Should().BeNull();
        reservation.ReleasedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidQuantity_ShouldFail(int quantity)
    {
        var result = InventoryReservation.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), quantity, DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarkAsConsumed_WhenReserved_ShouldTransitionToConsumed()
    {
        var reservation = InventoryReservation.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 2, DateTime.UtcNow).Value;
        var consumedAt = DateTime.UtcNow;

        var result = reservation.MarkAsConsumed(consumedAt);

        result.IsSuccess.Should().BeTrue();
        reservation.Status.Should().Be(InventoryReservationStatus.Consumed);
        reservation.ConsumedAt.Should().Be(consumedAt);
    }

    [Fact]
    public void Release_WhenReserved_ShouldTransitionToReleased()
    {
        var reservation = InventoryReservation.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 2, DateTime.UtcNow).Value;
        var releasedAt = DateTime.UtcNow;

        var result = reservation.Release(releasedAt);

        result.IsSuccess.Should().BeTrue();
        reservation.Status.Should().Be(InventoryReservationStatus.Released);
        reservation.ReleasedAt.Should().Be(releasedAt);
    }

    [Fact]
    public void MarkAsConsumed_WhenAlreadyConsumed_ShouldFail()
    {
        var reservation = InventoryReservation.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 2, DateTime.UtcNow).Value;
        reservation.MarkAsConsumed(DateTime.UtcNow);

        var result = reservation.MarkAsConsumed(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Release_WhenAlreadyReleased_ShouldFail()
    {
        var reservation = InventoryReservation.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 2, DateTime.UtcNow).Value;
        reservation.Release(DateTime.UtcNow);

        var result = reservation.Release(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
    }
}
