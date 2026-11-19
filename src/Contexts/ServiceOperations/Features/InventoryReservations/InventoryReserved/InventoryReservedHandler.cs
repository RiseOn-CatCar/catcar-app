namespace CatCar.Contexts.ServiceOperations.Features.InventoryReservations.InventoryReserved;

using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using Microsoft.Extensions.Logging;
using Wolverine.EntityFrameworkCore;

public static partial class InventoryReservedHandler
{
    public static async Task Handle(InventoryReservedIntegrationEvent message, IWorkOrderRepository workOrders, IBudgetRepository budgets, IDbContextOutbox<ServiceOperationsDbContext> outbox, ILogger<InventoryReservedLog> logger, CancellationToken cancellationToken)
    {
        var workOrder = await workOrders.GetByIdAsync(message.WorkOrderId, cancellationToken).ConfigureAwait(false);
        var budget = await budgets.GetByIdAsync(message.BudgetId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null || budget is null || budget.WorkOrderId != message.WorkOrderId || budget.Status != BudgetStatus.Approved || workOrder.Status != WorkOrderStatus.AwaitingApproval || workOrder.ActiveBudgetId != message.BudgetId || workOrder.ApprovedBudgetId != message.BudgetId)
        {
            LogDiscarded(logger, message.WorkOrderId, message.BudgetId);
            return;
        }

        var previousStartedAt = workOrder.HasCompleteStageHistory ? workOrder.StatusEnteredAt : null;
        var result = workOrder.ConfirmInventoryReservation(message.BudgetId, message.ReservedAt);
        if (result.IsFailure) throw new InvalidOperationException(result.Error.Message);
        await outbox.PublishAsync(new WorkOrderStatusChangedIntegrationEvent(workOrder.Id, workOrder.CustomerId, nameof(WorkOrderStatus.AwaitingApproval), workOrder.Status.ToString(), message.ReservedAt, message.CorrelationId, Guid.NewGuid(), previousStartedAt, message.BudgetId)).ConfigureAwait(false);
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Discarding inventory reservation confirmation for work order {WorkOrderId}, budget {BudgetId}")]
    private static partial void LogDiscarded(ILogger logger, Guid workOrderId, Guid budgetId);
}

public sealed class InventoryReservedLog { }
