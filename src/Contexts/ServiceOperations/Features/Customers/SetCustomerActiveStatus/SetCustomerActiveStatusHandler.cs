namespace CatCar.Contexts.ServiceOperations.Features.Customers.SetCustomerActiveStatus;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="SetCustomerActiveStatusCommand"/>.
/// </summary>
public static class SetCustomerActiveStatusHandler
{
    public static async Task<Upshot<SetCustomerActiveStatusResult>> Handle(
        SetCustomerActiveStatusCommand command,
        IValidator<SetCustomerActiveStatusCommand> validator,
        ICustomerRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<SetCustomerActiveStatusResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var customer = await repository.GetByIdAsync(command.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
            return Upshot<SetCustomerActiveStatusResult>.Fail("Cliente não encontrado.");

        customer.SetActiveStatus(command.IsActive);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<SetCustomerActiveStatusResult>.Success(new SetCustomerActiveStatusResult(customer.Id, customer.IsActive));
    }
}
