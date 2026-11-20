namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.StartDiagnosis;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.Saga;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contracts.ServiceOperations;
using FluentValidation;
using RiseOn.RailResult.Upshot;
using Wolverine.EntityFrameworkCore;

public static class StartDiagnosisHandler
{
    public static async Task<Upshot<string>> Handle(StartDiagnosisCommand command, IValidator<StartDiagnosisCommand> validator, IWorkOrderRepository repository, ICustomerRepository customers, IDbContextOutbox<ServiceOperationsDbContext> outbox, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid) return Upshot<string>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));
        var workOrder = await repository.GetByIdAsync(command.WorkOrderId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null) return Upshot<string>.Fail("OS não encontrada.");
        var customer = await customers.GetByIdAsync(workOrder.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null) return Upshot<string>.Fail("Customer not found.");
        var previousStatus = workOrder.Status.ToString();
        var previousStartedAt = workOrder.HasCompleteStageHistory ? workOrder.StatusEnteredAt : null;
        var transition = workOrder.StartDiagnosis(DateTime.UtcNow);
        if (transition.IsFailure) return Upshot<string>.Fail(transition.Error);
        await outbox.PublishAsync(new WorkOrderStatusChangedIntegrationEvent(workOrder.Id, workOrder.CustomerId, previousStatus, workOrder.Status.ToString(), workOrder.LastUpdatedAt, command.CorrelationId, Guid.NewGuid(), previousStartedAt, null)).ConfigureAwait(false);
        await outbox.PublishAsync(new WorkOrderStatusChanged(workOrder.Id, workOrder.CustomerId, customer.Name, customer.Email, previousStatus, workOrder.Status.ToString(), workOrder.LastUpdatedAt, command.CorrelationId, Guid.NewGuid())).ConfigureAwait(false);
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
        return Upshot<string>.Success(workOrder.Status.ToString());
    }
}
