namespace CatCar.Contexts.ServiceOperations.Features.Customers.RegisterCustomer;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="RegisterCustomerCommand"/>.
/// </summary>
public static class RegisterCustomerHandler
{
    public static async Task<Upshot<RegisterCustomerResult>> Handle(
        RegisterCustomerCommand command,
        IValidator<RegisterCustomerCommand> validator,
        ICustomerRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<RegisterCustomerResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var customerResult = Customer.Register(command.Document, command.Name, command.Phone, command.Email);
        if (customerResult.IsFailure)
            return Upshot<RegisterCustomerResult>.Fail(customerResult.Error);

        var customer = customerResult.Value;

        if (await repository.ExistsByDocumentAsync(customer.Document.Value, cancellationToken).ConfigureAwait(false))
            return Upshot<RegisterCustomerResult>.Fail("Já existe um cliente cadastrado com este CPF/CNPJ.");

        await repository.AddAsync(customer, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<RegisterCustomerResult>.Success(
            new RegisterCustomerResult(customer.Id, customer.Document.ToString(), customer.Name, customer.Phone, customer.Email, customer.IsActive));
    }
}
