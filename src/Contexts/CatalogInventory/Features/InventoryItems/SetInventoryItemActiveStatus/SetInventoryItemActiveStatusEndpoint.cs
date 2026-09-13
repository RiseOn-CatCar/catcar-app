namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.SetInventoryItemActiveStatus;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the endpoint that activates/deactivates a part/supply. Restricted to Administrador/Tecnico roles
/// (AC: CRUD de peças e insumos - feature 03).
/// </summary>
public static class SetInventoryItemActiveStatusEndpoint
{
    public static IEndpointRouteBuilder MapSetInventoryItemActiveStatusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/inventory-items/{inventoryItemId:guid}/status", async (Guid inventoryItemId, SetInventoryItemActiveStatusRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var command = new SetInventoryItemActiveStatusCommand(inventoryItemId, request.IsActive);
                var result = await bus.InvokeAsync<Upshot<SetInventoryItemActiveStatusResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao alterar status da peça/insumo");
            })
            .WithName("SetInventoryItemActiveStatus")
            .WithTags("CatalogInventory")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Tecnico"))
            .Produces<SetInventoryItemActiveStatusResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}

/// <summary>
/// Request body for the status-change endpoint (the id travels in the route).
/// </summary>
public sealed record SetInventoryItemActiveStatusRequest(bool IsActive);
