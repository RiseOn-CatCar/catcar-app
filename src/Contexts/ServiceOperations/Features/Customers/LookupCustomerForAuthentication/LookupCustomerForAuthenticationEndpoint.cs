namespace CatCar.Contexts.ServiceOperations.Features.Customers.LookupCustomerForAuthentication;

using System.Security.Cryptography;
using System.Text;
using CatCar.Contexts.ServiceOperations.Domain.Customers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

public static class LookupCustomerForAuthenticationEndpoint
{
    public static IEndpointRouteBuilder MapLookupCustomerForAuthenticationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/internal/v1/customers/by-document/{documentNumber}", HandleAsync)
            .WithName("LookupCustomerForAuthentication")
            .WithTags("Internal")
            .Produces<AuthenticationCustomer>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        return endpoints;
    }

    public static async Task<IResult> HandleAsync(string documentNumber, HttpRequest request, IOptions<InternalApiOptions> options, ICustomerRepository repository, CancellationToken cancellationToken)
    {
        var key = options.Value.Key;
        if (string.IsNullOrWhiteSpace(key) || !request.Headers.TryGetValue("X-Internal-Api-Key", out var supplied) || supplied.Count != 1 ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(supplied.ToString())))
            return Results.Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized);

        if (documentNumber.Length == 0 || documentNumber.Any(c => c is < '0' or > '9'))
            return Results.Problem(title: "Customer not found", statusCode: StatusCodes.Status404NotFound);

        var customer = await LookupCustomerForAuthenticationHandler.Handle(documentNumber, repository, cancellationToken).ConfigureAwait(false);
        return customer is null
            ? Results.Problem(title: "Customer not found", statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(customer);
    }
}
