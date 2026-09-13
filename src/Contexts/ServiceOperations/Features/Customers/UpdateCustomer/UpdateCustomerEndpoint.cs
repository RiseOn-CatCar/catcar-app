namespace CatCar.Contexts.ServiceOperations.Features.Customers.UpdateCustomer;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the customer update endpoint (feature 04: CRUD de clientes).
/// </summary>
public static class UpdateCustomerEndpoint
{
    public static IEndpointRouteBuilder MapUpdateCustomerEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/customers/{id:guid}", async (Guid id, UpdateCustomerRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var command = new UpdateCustomerCommand(id, request.Name, request.Phone, request.Email);
                var result = await bus.InvokeAsync<Upshot<UpdateCustomerResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao atualizar cliente");
            })
            .WithName("UpdateCustomer")
            .WithTags("ServiceOperations")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Recepcionista"))
            .Produces<UpdateCustomerResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}

/// <summary>
/// Request body for updating a customer's contact details.
/// </summary>
public sealed record UpdateCustomerRequest(string Name, string Phone, string? Email);
