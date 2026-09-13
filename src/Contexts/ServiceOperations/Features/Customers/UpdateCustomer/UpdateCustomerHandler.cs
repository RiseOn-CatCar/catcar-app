namespace CatCar.Contexts.ServiceOperations.Features.Customers.UpdateCustomer;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="UpdateCustomerCommand"/>.
/// </summary>
public static class UpdateCustomerHandler
{
    public static async Task<Upshot<UpdateCustomerResult>> Handle(
        UpdateCustomerCommand command,
        IValidator<UpdateCustomerCommand> validator,
        ICustomerRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<UpdateCustomerResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var customer = await repository.GetByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (customer is null)
            return Upshot<UpdateCustomerResult>.Fail("Cliente não encontrado.");

        var updateResult = customer.UpdateContactDetails(command.Name, command.Phone, command.Email);
        if (updateResult.IsFailure)
            return Upshot<UpdateCustomerResult>.Fail(updateResult.Error);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<UpdateCustomerResult>.Success(
            new UpdateCustomerResult(customer.Id, customer.Document.ToString(), customer.Name, customer.Phone, customer.Email, customer.IsActive));
    }
}
