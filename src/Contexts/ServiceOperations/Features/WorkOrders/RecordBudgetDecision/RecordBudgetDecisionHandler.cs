namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.RecordBudgetDecision;

using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contracts.ServiceOperations;
using FluentValidation;
using Wolverine.EntityFrameworkCore;

/// <summary>
/// In-process Wolverine handler for <see cref="RecordBudgetDecisionCommand"/>. Not exposed via HTTP -
/// this is the ServiceOperations side of the Communication -> ServiceOperations ACL call described in the
/// ratified design (feature 05): Communication has already authenticated the customer via the external
/// access token before invoking this command. Applies <see cref="Budget.Approve"/>/<see cref="Budget.Reject"/>
/// and <see cref="WorkOrder.RecordApproval"/> atomically and publishes <see cref="BudgetApprovedIntegrationEvent"/>
/// or <see cref="BudgetRejectedIntegrationEvent"/> through the transactional outbox (AC-017 pattern reused).
/// </summary>
public static class RecordBudgetDecisionHandler
{
    public static async Task<RecordBudgetDecisionResponse> Handle(
        RecordBudgetDecisionCommand command,
        IValidator<RecordBudgetDecisionCommand> validator,
        IBudgetRepository budgetRepository,
        IWorkOrderRepository workOrderRepository,
        IDbContextOutbox<ServiceOperationsDbContext> outbox,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Fail(command.BudgetId, string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var budget = await budgetRepository.GetByIdAsync(command.BudgetId, cancellationToken).ConfigureAwait(false);
        if (budget is null)
            return Fail(command.BudgetId, "Orçamento não encontrado.");

        var workOrder = await workOrderRepository.GetByIdAsync(budget.WorkOrderId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null)
            return Fail(command.BudgetId, "OS não encontrada.");

        var previousWorkOrderStatus = workOrder.Status.ToString();
        var budgetTransition = command.Approved ? budget.Approve() : budget.Reject(command.RejectionReason);
        if (budgetTransition.IsFailure)
            return Fail(command.BudgetId, budgetTransition.Error.Message ?? string.Empty);

        var workOrderTransition = workOrder.RecordApproval(command.Approved);
        if (workOrderTransition.IsFailure)
            return Fail(command.BudgetId, workOrderTransition.Error.Message ?? string.Empty);

        if (command.Approved)
        {
            await outbox.PublishAsync(new BudgetApprovedIntegrationEvent(
                budget.Id, workOrder.Id, workOrder.CustomerId, budget.TotalAmount, DateTime.UtcNow)).ConfigureAwait(false);
        }
        else
        {
            await outbox.PublishAsync(new BudgetRejectedIntegrationEvent(
                budget.Id, workOrder.Id, workOrder.CustomerId, command.RejectionReason ?? string.Empty, DateTime.UtcNow)).ConfigureAwait(false);
        }

        await outbox.PublishAsync(new WorkOrderStatusChangedIntegrationEvent(
            workOrder.Id,
            workOrder.CustomerId,
            previousWorkOrderStatus,
            workOrder.Status.ToString(),
            workOrder.LastUpdatedAt,
            Guid.NewGuid())).ConfigureAwait(false);

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);

        return new RecordBudgetDecisionResponse(
            Success: true,
            Error: null,
            BudgetId: budget.Id,
            WorkOrderId: workOrder.Id,
            BudgetStatus: budget.Status.ToString(),
            WorkOrderStatus: workOrder.Status.ToString());
    }

    private static RecordBudgetDecisionResponse Fail(Guid budgetId, string error)
        => new(Success: false, Error: error, BudgetId: budgetId, WorkOrderId: Guid.Empty, BudgetStatus: string.Empty, WorkOrderStatus: string.Empty);
}
