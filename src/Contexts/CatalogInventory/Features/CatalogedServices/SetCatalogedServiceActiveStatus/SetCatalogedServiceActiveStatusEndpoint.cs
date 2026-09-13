namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.SetCatalogedServiceActiveStatus;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the endpoint that activates/deactivates a cataloged service. Restricted to Administrador/Tecnico roles
/// (AC: CRUD de serviços - feature 03).
/// </summary>
public static class SetCatalogedServiceActiveStatusEndpoint
{
    public static IEndpointRouteBuilder MapSetCatalogedServiceActiveStatusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/services/{serviceId:guid}/status", async (Guid serviceId, SetCatalogedServiceActiveStatusRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var command = new SetCatalogedServiceActiveStatusCommand(serviceId, request.IsActive);
                var result = await bus.InvokeAsync<Upshot<SetCatalogedServiceActiveStatusResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao alterar status do serviço");
            })
            .WithName("SetCatalogedServiceActiveStatus")
            .WithTags("CatalogInventory")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Tecnico"))
            .Produces<SetCatalogedServiceActiveStatusResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}

/// <summary>
/// Request body for the status-change endpoint (the id travels in the route).
/// </summary>
public sealed record SetCatalogedServiceActiveStatusRequest(bool IsActive);
