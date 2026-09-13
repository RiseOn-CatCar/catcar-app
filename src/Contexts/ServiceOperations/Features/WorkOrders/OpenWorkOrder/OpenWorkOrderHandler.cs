namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.OpenWorkOrder;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="OpenWorkOrderCommand"/>.
/// </summary>
public static class OpenWorkOrderHandler
{
    public static async Task<Upshot<OpenWorkOrderResult>> Handle(
        OpenWorkOrderCommand command,
        IValidator<OpenWorkOrderCommand> validator,
        ICustomerRepository customerRepository,
        IVehicleRepository vehicleRepository,
        IWorkOrderRepository workOrderRepository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<OpenWorkOrderResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var customer = await customerRepository.GetByIdAsync(command.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
            return Upshot<OpenWorkOrderResult>.Fail("Cliente não encontrado.");

        var vehicle = await vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken).ConfigureAwait(false);
        if (vehicle is null)
            return Upshot<OpenWorkOrderResult>.Fail("Veículo não encontrado.");

        if (vehicle.CustomerId != customer.Id)
            return Upshot<OpenWorkOrderResult>.Fail("O veículo informado não pertence ao cliente informado.");

        var workOrderResult = WorkOrder.Open(command.CustomerId, command.VehicleId, command.InitialDescription);
        if (workOrderResult.IsFailure)
            return Upshot<OpenWorkOrderResult>.Fail(workOrderResult.Error);

        var workOrder = workOrderResult.Value;

        await workOrderRepository.AddAsync(workOrder, cancellationToken).ConfigureAwait(false);
        await workOrderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<OpenWorkOrderResult>.Success(
            new OpenWorkOrderResult(workOrder.Id, workOrder.CustomerId, workOrder.VehicleId, workOrder.InitialDescription, workOrder.Status.ToString(), workOrder.OpenedAt));
    }
}
