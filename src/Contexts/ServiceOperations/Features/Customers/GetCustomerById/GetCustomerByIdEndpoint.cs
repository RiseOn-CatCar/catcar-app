namespace CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerById;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the customer detail endpoint (feature 04: consulta de cliente por id).
/// </summary>
public static class GetCustomerByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetCustomerByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/customers/{customerId:guid}", async (Guid customerId, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<CustomerDetails>>(new GetCustomerByIdQuery(customerId), cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status404NotFound, title: "Cliente não encontrado");
            })
            .WithName("GetCustomerById")
            .WithTags("ServiceOperations")
            .RequireAuthorization()
            .Produces<CustomerDetails>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
