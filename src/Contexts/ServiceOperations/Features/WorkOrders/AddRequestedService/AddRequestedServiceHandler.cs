namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedService;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Integrations;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="AddRequestedServiceCommand"/>.
/// Uses <see cref="ICatalogInventoryAcl"/> to obtain the price/description snapshot (cross-BC boundary).
/// </summary>
public static class AddRequestedServiceHandler
{
    public static async Task<Upshot<AddRequestedServiceResult>> Handle(
        AddRequestedServiceCommand command,
        IValidator<AddRequestedServiceCommand> validator,
        IWorkOrderRepository repository,
        ICatalogInventoryAcl catalogInventoryAcl,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<AddRequestedServiceResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var workOrder = await repository.GetByIdAsync(command.WorkOrderId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null)
            return Upshot<AddRequestedServiceResult>.Fail("OS não encontrada.");

        var snapshotResult = await catalogInventoryAcl.GetCatalogedServiceSnapshotAsync(command.CatalogedServiceId, cancellationToken).ConfigureAwait(false);
        if (snapshotResult.IsFailure)
            return Upshot<AddRequestedServiceResult>.Fail(snapshotResult.Error);

        var snapshot = snapshotResult.Value;

        var addResult = workOrder.AddRequestedService(snapshot.Id, snapshot.Description, snapshot.UnitPrice, command.Quantity);
        if (addResult.IsFailure)
            return Upshot<AddRequestedServiceResult>.Fail(addResult.Error);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var line = workOrder.RequestedServices[^1];

        return Upshot<AddRequestedServiceResult>.Success(
            new AddRequestedServiceResult(workOrder.Id, line.CatalogedServiceId, line.Description, line.UnitPrice, line.Quantity, line.LineTotal));
    }
}
