namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.IssueBudget;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.Saga;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RiseOn.RailResult.Upshot;
using Wolverine;
using Wolverine.EntityFrameworkCore;

public static class IssueBudgetHandler
{
    public static async Task<Upshot> Handle(
        IssueBudgetCommand command,
        IValidator<IssueBudgetCommand> validator,
        IWorkOrderRepository workOrderRepository,
        ICustomerRepository customerRepository,
        IDbContextOutbox<ServiceOperationsDbContext> outbox,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));
        var order = await workOrderRepository.GetByIdAsync(command.WorkOrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
            return Upshot.Fail("OS não encontrada.");
        if (order.Status != WorkOrderStatus.InDiagnosis || (order.RequestedServices.Count == 0 && order.RequestedParts.Count == 0))
            return Upshot.Fail("A work order in diagnosis with at least one line is required.");
        var customer = await customerRepository.GetByIdAsync(order.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || !CustomerEmailValidation.IsValid(customer.Email))
            return Upshot.Fail("A valid customer email is required to issue a budget.");
        if (await outbox.DbContext.Set<WorkOrderSaga>().AnyAsync(x => x.Id == order.Id, cancellationToken).ConfigureAwait(false))
            return Upshot.Fail("A budget request is already in progress.");
        var lines = order.RequestedServices.Select(x => new BudgetLineRequest("Service", x.CatalogedServiceId, x.Description, x.UnitPrice, x.Quantity))
            .Concat(order.RequestedParts.Select(x => new BudgetLineRequest("Part", x.InventoryItemId, x.Description, x.UnitPrice, x.Quantity))).ToArray();
        var now = DateTime.UtcNow;
        outbox.DbContext.Set<WorkOrderSaga>().Add(new WorkOrderSaga { Id = order.Id, CorrelationId = command.CorrelationId });
        await outbox.PublishAsync(new RequestBudget(order.Id, customer.Id, customer.Name, customer.Email!, lines, now, command.CorrelationId)).ConfigureAwait(false);
        var delay = WorkOrderSaga.Timeout(configuration, "ApprovalTimeout", 72);
        await outbox.ScheduleAsync(new ApprovalTimeout(order.Id, command.CorrelationId, delay), delay).ConfigureAwait(false);
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
        return Upshot.Success();
    }
}
