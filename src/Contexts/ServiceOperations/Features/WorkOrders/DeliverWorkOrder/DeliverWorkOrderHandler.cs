namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.DeliverWorkOrder;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contracts.ServiceOperations;
using FluentValidation;
using RiseOn.RailResult.Upshot;
using Wolverine.EntityFrameworkCore;

public static class DeliverWorkOrderHandler
{
    public static async Task<Upshot<string>> Handle(DeliverWorkOrderCommand command, IValidator<DeliverWorkOrderCommand> validator, IWorkOrderRepository repository, IDbContextOutbox<ServiceOperationsDbContext> outbox, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid) return Upshot<string>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));
        var workOrder = await repository.GetByIdAsync(command.WorkOrderId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null) return Upshot<string>.Fail("OS não encontrada.");
        var previousStatus = workOrder.Status.ToString();
        var transition = workOrder.Deliver();
        if (transition.IsFailure) return Upshot<string>.Fail(transition.Error);
        await outbox.PublishAsync(new WorkOrderStatusChangedIntegrationEvent(workOrder.Id, workOrder.CustomerId, previousStatus, workOrder.Status.ToString(), workOrder.LastUpdatedAt, command.CorrelationId == Guid.Empty ? Guid.NewGuid() : command.CorrelationId)).ConfigureAwait(false);
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
        return Upshot<string>.Success(workOrder.Status.ToString());
    }
}
