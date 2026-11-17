namespace CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wolverine.EntityFrameworkCore;

public sealed class BudgetReservationOutcomeConflictException(Exception innerException)
    : Exception("A concurrent reservation outcome was committed.", innerException)
{
}

internal static class InventoryOutbox
{
    public static async Task SaveAsync(IDbContextOutbox<CatalogInventoryDbContext> outbox, CancellationToken cancellationToken)
    {
        try
        {
            await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "pk_budget_reservation_outcomes" or "IX_inventory_reservations_budget_id_inventory_item_id"
        })
        {
            throw new BudgetReservationOutcomeConflictException(exception);
        }
    }
}
