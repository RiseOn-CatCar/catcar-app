namespace CatCar.Contexts.ServiceOperations.Integrations;

using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contracts.ServiceOperations;

/// <summary>
/// Wolverine handler answering the published-language <see cref="GetBudgetSnapshotQuery"/> defined in
/// <c>CatCar.Contracts.ServiceOperations</c>. This is the Open Host Service (OHS) side of the
/// Customer-Supplier relationship with Communication (feature 05): the Communication context invokes
/// this query via <c>IMessageBus</c> without ever referencing the ServiceOperations assembly directly,
/// to read the budget's frozen lines and the customer's contact details needed for the approval-link e-mail.
/// </summary>
public static class BudgetSnapshotQueryHandler
{
    public static async Task<BudgetSnapshotResponse> Handle(
        GetBudgetSnapshotQuery query,
        IBudgetRepository budgetRepository,
        IWorkOrderRepository workOrderRepository,
        ICustomerRepository customerRepository,
        CancellationToken cancellationToken)
    {
        var budget = await budgetRepository.GetByIdAsync(query.BudgetId, cancellationToken).ConfigureAwait(false);
        if (budget is null)
            return NotFound(query.BudgetId);

        var workOrder = await workOrderRepository.GetByIdAsync(budget.WorkOrderId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null)
            return NotFound(query.BudgetId);

        var customer = await customerRepository.GetByIdAsync(workOrder.CustomerId, cancellationToken).ConfigureAwait(false);

        var lines = budget.Lines
            .Select(l => new BudgetSnapshotLine(l.Type.ToString(), l.Description, l.UnitPrice, l.Quantity, l.LineTotal, l.ReferenceId))
            .ToList();

        return new BudgetSnapshotResponse(
            Found: true,
            BudgetId: budget.Id,
            WorkOrderId: workOrder.Id,
            CustomerId: workOrder.CustomerId,
            CustomerName: customer?.Name ?? string.Empty,
            CustomerEmail: customer?.Email,
            BudgetStatus: budget.Status.ToString(),
            TotalAmount: budget.TotalAmount,
            IssuedAt: budget.IssuedAt,
            Lines: lines);
    }

    private static BudgetSnapshotResponse NotFound(Guid budgetId) =>
        new(false, budgetId, Guid.Empty, Guid.Empty, string.Empty, null, string.Empty, 0m, default, []);
}
