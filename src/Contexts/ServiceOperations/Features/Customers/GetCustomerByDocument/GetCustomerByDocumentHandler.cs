namespace CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerByDocument;

using System.Linq;
using CatCar.Contexts.ServiceOperations.Domain.Customers;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="GetCustomerByDocumentQuery"/>.
/// </summary>
public static class GetCustomerByDocumentHandler
{
    public static async Task<Upshot<CustomerByDocumentDetails>> Handle(
        GetCustomerByDocumentQuery query,
        ICustomerRepository repository,
        CancellationToken cancellationToken)
    {
        var digits = new string((query.Document ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return Upshot<CustomerByDocumentDetails>.Fail("O documento (CPF/CNPJ) é obrigatório.");

        var customer = await repository.GetByDocumentAsync(digits, cancellationToken).ConfigureAwait(false);
        if (customer is null)
            return Upshot<CustomerByDocumentDetails>.Fail("Cliente não encontrado.");

        return Upshot<CustomerByDocumentDetails>.Success(
            new CustomerByDocumentDetails(customer.Id, customer.Document.ToString(), customer.Name, customer.Phone, customer.Email, customer.IsActive));
    }
}
