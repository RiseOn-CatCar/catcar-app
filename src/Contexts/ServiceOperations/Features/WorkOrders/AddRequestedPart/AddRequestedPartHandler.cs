namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedPart;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Integrations;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="AddRequestedPartCommand"/>.
/// Uses <see cref="ICatalogInventoryAcl"/> to obtain the price/description/stock snapshot (cross-BC boundary).
/// Actual stock reservation/consumption is deferred to feature 06 (acompanhamento-execucao-e-priorizacao-os).
/// </summary>
public static class AddRequestedPartHandler
{
    public static async Task<Upshot<AddRequestedPartResult>> Handle(
        AddRequestedPartCommand command,
        IValidator<AddRequestedPartCommand> validator,
        IWorkOrderRepository repository,
        ICatalogInventoryAcl catalogInventoryAcl,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<AddRequestedPartResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var workOrder = await repository.GetByIdAsync(command.WorkOrderId, cancellationToken).ConfigureAwait(false);
        if (workOrder is null)
            return Upshot<AddRequestedPartResult>.Fail("OS não encontrada.");

        var snapshotResult = await catalogInventoryAcl.GetInventoryItemSnapshotAsync(command.InventoryItemId, cancellationToken).ConfigureAwait(false);
        if (snapshotResult.IsFailure)
            return Upshot<AddRequestedPartResult>.Fail(snapshotResult.Error);

        var snapshot = snapshotResult.Value;

        if (snapshot.QuantityInStock < command.Quantity)
            return Upshot<AddRequestedPartResult>.Fail("Estoque insuficiente para a quantidade solicitada da peça/insumo.");

        var addResult = workOrder.AddRequestedPart(snapshot.Id, snapshot.Description, snapshot.UnitPrice, command.Quantity);
        if (addResult.IsFailure)
            return Upshot<AddRequestedPartResult>.Fail(addResult.Error);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var line = workOrder.RequestedParts[^1];

        return Upshot<AddRequestedPartResult>.Success(
            new AddRequestedPartResult(workOrder.Id, line.InventoryItemId, line.Description, line.UnitPrice, line.Quantity, line.LineTotal));
    }
}
