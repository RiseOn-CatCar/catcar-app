namespace CatCar.Api;

using System.Diagnostics;
using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;

/// <summary>Replays legacy approved budgets before work-order mutations are resumed.</summary>
public static class InventoryReservationReconciler
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var budgetIds = new List<Guid>();
        var publishScope = services.CreateAsyncScope();
        await using (publishScope.ConfigureAwait(false))
        {
            var db = publishScope.ServiceProvider.GetRequiredService<ServiceOperationsDbContext>();
            var outbox = publishScope.ServiceProvider.GetRequiredService<IDbContextOutbox<ServiceOperationsDbContext>>();
            var orders = await db.WorkOrders
                .Where(x => x.Status == WorkOrderStatus.InExecution && !x.HasCompleteStageHistory)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var order in orders)
            {
                if (order.ActiveBudgetId is not Guid budgetId || order.ApprovedBudgetId != budgetId)
                    throw new InvalidOperationException($"Legacy work order {order.Id} has an ambiguous approved budget.");

                var budget = await db.Budgets.Include(x => x.Lines)
                    .SingleOrDefaultAsync(x => x.Id == budgetId && x.WorkOrderId == order.Id, cancellationToken)
                    .ConfigureAwait(false);
                if (budget is null || budget.Status != BudgetStatus.Approved || order.BudgetApprovedAt is null)
                    throw new InvalidOperationException($"Legacy work order {order.Id} has no unambiguous approved budget.");

                budgetIds.Add(budgetId);
                await outbox.PublishAsync(new BudgetApprovedIntegrationEvent(
                    budgetId, order.Id, order.CustomerId, budget.TotalAmount, order.BudgetApprovedAt.Value, Guid.NewGuid())).ConfigureAwait(false);
            }

            if (budgetIds.Count != 0)
                await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
        }

        var pending = budgetIds.ToHashSet();
        var timer = Stopwatch.StartNew();
        while (pending.Count != 0 && timer.Elapsed < TimeSpan.FromSeconds(120))
        {
            var scope = services.CreateAsyncScope();
            await using var scopeDisposal = scope.ConfigureAwait(false);
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            var db = scope.ServiceProvider.GetRequiredService<ServiceOperationsDbContext>();
            var outbox = scope.ServiceProvider.GetRequiredService<IDbContextOutbox<ServiceOperationsDbContext>>();
            foreach (var batch in pending.Chunk(100).ToArray())
            {
                var outcomes = await bus.InvokeAsync<IReadOnlyList<BudgetReservationOutcomeResponse>>(
                    new GetBudgetReservationOutcomesQuery(batch), cancellationToken).ConfigureAwait(false);
                foreach (var outcome in outcomes)
                {
                    if (!pending.Contains(outcome.BudgetId))
                        throw new InvalidOperationException("Inventory reconciliation returned an unexpected budget.");
                    if (outcome.Outcome is "Reserved" or "NoParts")
                    {
                        pending.Remove(outcome.BudgetId);
                        continue;
                    }
                    if (outcome.Outcome != "Failed" || outcome.FailureCode is not ("InsufficientStock" or "InventoryItemNotFound" or "InventoryItemInactive"))
                        throw new InvalidOperationException($"Inventory reconciliation blocked for budget {outcome.BudgetId}: {outcome.FailureCode ?? outcome.Outcome}.");

                    var order = await db.WorkOrders.SingleAsync(x => x.ApprovedBudgetId == outcome.BudgetId, cancellationToken)
                        .ConfigureAwait(false);
                    var transition = order.ReconcileInventoryReservationFailure(outcome.BudgetId, outcome.FailureCode, outcome.ResolvedAt);
                    if (transition.IsFailure)
                        throw new InvalidOperationException($"Inventory reconciliation failed for budget {outcome.BudgetId}: {transition.Error.Message}");
                    await outbox.PublishAsync(new WorkOrderStatusChangedIntegrationEvent(
                        order.Id, order.CustomerId, nameof(WorkOrderStatus.InExecution), order.Status.ToString(),
                        order.LastUpdatedAt, Guid.NewGuid(), Guid.NewGuid(), null, outcome.BudgetId)).ConfigureAwait(false);
                    await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
                    pending.Remove(outcome.BudgetId);
                }
            }
            if (pending.Count != 0)
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
        }

        if (pending.Count != 0)
            throw new TimeoutException($"Inventory reconciliation timed out with {pending.Count} unresolved budgets.");
    }
}
