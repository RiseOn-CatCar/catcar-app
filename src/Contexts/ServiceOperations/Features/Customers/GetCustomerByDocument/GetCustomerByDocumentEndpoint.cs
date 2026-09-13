namespace CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerByDocument;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the customer lookup-by-document endpoint (feature 04: identificação do cliente por CPF/CNPJ).
/// </summary>
public static class GetCustomerByDocumentEndpoint
{
    public static IEndpointRouteBuilder MapGetCustomerByDocumentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/customers/by-document/{document}", async (string document, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<CustomerByDocumentDetails>>(new GetCustomerByDocumentQuery(document), cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status404NotFound, title: "Cliente não encontrado");
            })
            .WithName("GetCustomerByDocument")
            .WithTags("ServiceOperations")
            .RequireAuthorization()
            .Produces<CustomerByDocumentDetails>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
