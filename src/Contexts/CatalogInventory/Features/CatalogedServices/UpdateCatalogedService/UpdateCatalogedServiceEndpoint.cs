namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.UpdateCatalogedService;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the cataloged-service update endpoint. Restricted to Administrador/Tecnico roles
/// (AC: CRUD de serviços - feature 03).
/// </summary>
public static class UpdateCatalogedServiceEndpoint
{
    public static IEndpointRouteBuilder MapUpdateCatalogedServiceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/services/{serviceId:guid}", async (Guid serviceId, UpdateCatalogedServiceRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var command = new UpdateCatalogedServiceCommand(serviceId, request.Name, request.Description, request.EstimatedDurationMinutes, request.Price);
                var result = await bus.InvokeAsync<Upshot<UpdateCatalogedServiceResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao atualizar serviço");
            })
            .WithName("UpdateCatalogedService")
            .WithTags("CatalogInventory")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Tecnico"))
            .Produces<UpdateCatalogedServiceResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}

/// <summary>
/// Request body for the cataloged-service update endpoint (the id travels in the route).
/// </summary>
public sealed record UpdateCatalogedServiceRequest(string Name, string Description, int EstimatedDurationMinutes, decimal Price);
