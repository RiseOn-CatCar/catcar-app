namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.UpdateInventoryItem;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the part/supply update endpoint. Restricted to Administrador/Tecnico roles
/// (AC: CRUD de peças e insumos - feature 03).
/// </summary>
public static class UpdateInventoryItemEndpoint
{
    public static IEndpointRouteBuilder MapUpdateInventoryItemEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/inventory-items/{inventoryItemId:guid}", async (Guid inventoryItemId, UpdateInventoryItemRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var command = new UpdateInventoryItemCommand(inventoryItemId, request.Name, request.Description, request.UnitPrice, request.MinimumStockThreshold);
                var result = await bus.InvokeAsync<Upshot<UpdateInventoryItemResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao atualizar peça/insumo");
            })
            .WithName("UpdateInventoryItem")
            .WithTags("CatalogInventory")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Tecnico"))
            .Produces<UpdateInventoryItemResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}

/// <summary>
/// Request body for the part/supply update endpoint (the id travels in the route).
/// </summary>
public sealed record UpdateInventoryItemRequest(string Name, string Description, decimal UnitPrice, int MinimumStockThreshold);
