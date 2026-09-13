namespace CatCar.Contexts.ServiceOperations.Features.Customers.SetCustomerActiveStatus;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the endpoint that activates/deactivates a customer (feature 04: CRUD de clientes).
/// </summary>
public static class SetCustomerActiveStatusEndpoint
{
    public static IEndpointRouteBuilder MapSetCustomerActiveStatusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/customers/{customerId:guid}/status", async (Guid customerId, SetCustomerActiveStatusRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var command = new SetCustomerActiveStatusCommand(customerId, request.IsActive);
                var result = await bus.InvokeAsync<Upshot<SetCustomerActiveStatusResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao alterar status do cliente");
            })
            .WithName("SetCustomerActiveStatus")
            .WithTags("ServiceOperations")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Recepcionista"))
            .Produces<SetCustomerActiveStatusResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}

/// <summary>
/// Request body for the status-change endpoint (the id travels in the route).
/// </summary>
public sealed record SetCustomerActiveStatusRequest(bool IsActive);
