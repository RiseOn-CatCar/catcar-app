namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.OpenWorkOrder;

using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contexts.ServiceOperations.Integrations;
using CatCar.Contracts.ServiceOperations;
using FluentValidation;
using RiseOn.RailResult.Upshot;
using Wolverine.EntityFrameworkCore;

/// <summary>
/// Wolverine handler for <see cref="OpenWorkOrderCommand"/>.
/// </summary>
public static class OpenWorkOrderHandler
{
#pragma warning disable S107 // Method parameters are resolved and injected by Wolverine message bus
    public static async Task<Upshot<OpenWorkOrderResult>> Handle(
        OpenWorkOrderCommand command,
        IValidator<OpenWorkOrderCommand> validator,
        ICustomerRepository customerRepository,
        IVehicleRepository vehicleRepository,
        IWorkOrderRepository workOrderRepository,
        IBudgetRepository budgetRepository,
        ICatalogInventoryAcl catalogInventoryAcl,
        IDbContextOutbox<ServiceOperationsDbContext> outbox,
        CancellationToken cancellationToken)
#pragma warning restore S107
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<OpenWorkOrderResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var validateCustomerResult = await ValidateCustomerAndVehicleAsync(command, customerRepository, vehicleRepository, cancellationToken).ConfigureAwait(false);
        if (validateCustomerResult.IsFailure)
            return Upshot<OpenWorkOrderResult>.Fail(validateCustomerResult.Error);

        var workOrderResult = WorkOrder.Open(command.CustomerId, command.VehicleId, command.InitialDescription);
        if (workOrderResult.IsFailure)
            return Upshot<OpenWorkOrderResult>.Fail(workOrderResult.Error);

        var workOrder = workOrderResult.Value;

        var populateServicesResult = await PopulateServicesAsync(workOrder, command.Services, catalogInventoryAcl, cancellationToken).ConfigureAwait(false);
        if (populateServicesResult.IsFailure)
            return Upshot<OpenWorkOrderResult>.Fail(populateServicesResult.Error);

        var populatePartsResult = await PopulatePartsAsync(workOrder, command.Parts, catalogInventoryAcl, cancellationToken).ConfigureAwait(false);
        if (populatePartsResult.IsFailure)
            return Upshot<OpenWorkOrderResult>.Fail(populatePartsResult.Error);

        await workOrderRepository.AddAsync(workOrder, cancellationToken).ConfigureAwait(false);

        var budgetResult = await TryCreateInitialBudgetAsync(workOrder, budgetRepository, outbox, cancellationToken).ConfigureAwait(false);
        if (budgetResult.IsFailure)
            return Upshot<OpenWorkOrderResult>.Fail(budgetResult.Error);

        await outbox.PublishAsync(new WorkOrderStatusChangedIntegrationEvent(
            workOrder.Id, workOrder.CustomerId, "None", workOrder.Status.ToString(), workOrder.LastUpdatedAt, Guid.NewGuid())).ConfigureAwait(false);
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<OpenWorkOrderResult>.Success(
            new OpenWorkOrderResult(workOrder.Id, workOrder.CustomerId, workOrder.VehicleId, workOrder.InitialDescription, workOrder.Status.ToString(), workOrder.OpenedAt));
    }

    private static async Task<Upshot> ValidateCustomerAndVehicleAsync(
        OpenWorkOrderCommand command,
        ICustomerRepository customerRepository,
        IVehicleRepository vehicleRepository,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(command.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
            return Upshot.Fail("Cliente não encontrado.");

        var vehicle = await vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken).ConfigureAwait(false);
        if (vehicle is null)
            return Upshot.Fail("Veículo não encontrado.");

        if (vehicle.CustomerId != customer.Id)
            return Upshot.Fail("O veículo informado não pertence ao cliente informado.");

        return Upshot.Success();
    }

    private static async Task<Upshot> PopulateServicesAsync(
        WorkOrder workOrder,
        IReadOnlyList<OpenWorkOrderItemDto>? services,
        ICatalogInventoryAcl acl,
        CancellationToken cancellationToken)
    {
        if (services is null) return Upshot.Success();

        foreach (var item in services)
        {
            var snapshotResult = await acl.GetCatalogedServiceSnapshotAsync(item.Id, cancellationToken).ConfigureAwait(false);
            if (snapshotResult.IsFailure)
                return Upshot.Fail(snapshotResult.Error);

            var snapshot = snapshotResult.Value;
            var addResult = workOrder.AddRequestedService(snapshot.Id, snapshot.Description, snapshot.UnitPrice, item.Quantity);
            if (addResult.IsFailure)
                return Upshot.Fail(addResult.Error);
        }

        return Upshot.Success();
    }

    private static async Task<Upshot> PopulatePartsAsync(
        WorkOrder workOrder,
        IReadOnlyList<OpenWorkOrderItemDto>? parts,
        ICatalogInventoryAcl acl,
        CancellationToken cancellationToken)
    {
        if (parts is null) return Upshot.Success();

        foreach (var item in parts)
        {
            var snapshotResult = await acl.GetInventoryItemSnapshotAsync(item.Id, cancellationToken).ConfigureAwait(false);
            if (snapshotResult.IsFailure)
                return Upshot.Fail(snapshotResult.Error);

            var snapshot = snapshotResult.Value;
            var addResult = workOrder.AddRequestedPart(snapshot.Id, snapshot.Description, snapshot.UnitPrice, item.Quantity);
            if (addResult.IsFailure)
                return Upshot.Fail(addResult.Error);
        }

        return Upshot.Success();
    }

    private static async Task<Upshot> TryCreateInitialBudgetAsync(
        WorkOrder workOrder,
        IBudgetRepository budgetRepository,
        IDbContextOutbox<ServiceOperationsDbContext> outbox,
        CancellationToken cancellationToken)
    {
        if (workOrder.RequestedServices.Count == 0 && workOrder.RequestedParts.Count == 0)
            return Upshot.Success();

        var lines = workOrder.RequestedServices.Select(s =>
            (BudgetLineType.Service, s.CatalogedServiceId, s.Description, s.UnitPrice, s.Quantity))
            .Concat(workOrder.RequestedParts.Select(p =>
                (BudgetLineType.Part, p.InventoryItemId, p.Description, p.UnitPrice, p.Quantity)))
            .ToList();

        var budgetResult = Budget.Issue(workOrder.Id, lines);
        if (budgetResult.IsFailure)
            return Upshot.Fail(budgetResult.Error);

        var budget = budgetResult.Value;
        var markResult = workOrder.MarkBudgetIssued(budget.Id);
        if (markResult.IsFailure)
            return Upshot.Fail(markResult.Error);

        await budgetRepository.AddAsync(budget, cancellationToken).ConfigureAwait(false);
        await outbox.PublishAsync(new BudgetIssuedIntegrationEvent(
            budget.Id, workOrder.Id, workOrder.CustomerId, budget.TotalAmount, budget.IssuedAt)).ConfigureAwait(false);

        return Upshot.Success();
    }
}
