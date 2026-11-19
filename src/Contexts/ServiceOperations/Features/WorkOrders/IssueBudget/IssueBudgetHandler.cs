namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.IssueBudget;

using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contracts.ServiceOperations;
using FluentValidation;
using RiseOn.RailResult.Upshot;
using Wolverine.EntityFrameworkCore;

/// <summary>
/// Wolverine handler for <see cref="IssueBudgetCommand"/>. Freezes a price/description snapshot of the
/// WorkOrder's requested services/parts into a new <see cref="Budget"/>, marks the WorkOrder as awaiting
/// approval, and publishes <see cref="BudgetIssuedIntegrationEvent"/> through Wolverine's transactional
/// outbox so both the aggregate mutations and the integration event commit atomically (AC-017).
/// </summary>
public static class IssueBudgetHandler
{
    public static async Task<Upshot<IssueBudgetResult>> Handle(
        IssueBudgetCommand command,
        IValidator<IssueBudgetCommand> validator,
        IWorkOrderRepository workOrderRepository,
        ICustomerRepository customerRepository,
        IBudgetRepository budgetRepository,
        IDbContextOutbox<ServiceOperationsDbContext> outbox,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<IssueBudgetResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var workOrder = await workOrderRepository.GetByIdAsync(command.WorkOrderId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null)
            return Upshot<IssueBudgetResult>.Fail("OS não encontrada.");

        var customer = await customerRepository.GetByIdAsync(workOrder.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || !CustomerEmailValidation.IsValid(customer.Email))
            return Upshot<IssueBudgetResult>.Fail("A valid customer email is required to issue a budget.");

        var lines = new List<(BudgetLineType Type, Guid ReferenceId, string Description, decimal UnitPrice, int Quantity)>();
        lines.AddRange(workOrder.RequestedServices.Select(s =>
            (BudgetLineType.Service, s.CatalogedServiceId, s.Description, s.UnitPrice, s.Quantity)));
        lines.AddRange(workOrder.RequestedParts.Select(p =>
            (BudgetLineType.Part, p.InventoryItemId, p.Description, p.UnitPrice, p.Quantity)));

        var now = DateTime.UtcNow;
        var budgetResult = Budget.Issue(workOrder.Id, lines, now);
        if (budgetResult.IsFailure)
            return Upshot<IssueBudgetResult>.Fail(budgetResult.Error);

        var budget = budgetResult.Value;

        var previousWorkOrderStatus = workOrder.Status.ToString();
        var previousStartedAt = workOrder.HasCompleteStageHistory ? workOrder.StatusEnteredAt : null;
        var markResult = workOrder.MarkBudgetIssued(budget.Id, now);
        if (markResult.IsFailure)
            return Upshot<IssueBudgetResult>.Fail(markResult.Error);

        await budgetRepository.AddAsync(budget, cancellationToken).ConfigureAwait(false);

        var integrationEvent = new BudgetIssuedIntegrationEvent(
            budget.Id, workOrder.Id, workOrder.CustomerId, budget.TotalAmount, budget.IssuedAt, command.CorrelationId);

        await outbox.PublishAsync(integrationEvent).ConfigureAwait(false);
        await outbox.PublishAsync(new WorkOrderStatusChangedIntegrationEvent(
            workOrder.Id,
            workOrder.CustomerId,
            previousWorkOrderStatus,
            workOrder.Status.ToString(),
            workOrder.LastUpdatedAt,
            command.CorrelationId, Guid.NewGuid(), previousStartedAt, budget.Id)).ConfigureAwait(false);
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);

        var lineResults = budget.Lines
            .Select(l => new IssueBudgetLineResult(l.Type.ToString(), l.ReferenceId, l.Description, l.UnitPrice, l.Quantity, l.LineTotal))
            .ToList();

        return Upshot<IssueBudgetResult>.Success(
            new IssueBudgetResult(budget.Id, budget.WorkOrderId, budget.Status.ToString(), budget.TotalAmount, budget.IssuedAt, lineResults));
    }
}
