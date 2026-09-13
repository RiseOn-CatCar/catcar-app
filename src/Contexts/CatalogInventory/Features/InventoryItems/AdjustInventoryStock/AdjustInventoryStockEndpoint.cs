namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.AdjustInventoryStock;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the stock-movement endpoint (entrada/saída). Restricted to Administrador/Tecnico roles
/// (AC: controle de estoque - feature 03).
/// </summary>
public static class AdjustInventoryStockEndpoint
{
    public static IEndpointRouteBuilder MapAdjustInventoryStockEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/inventory-items/{inventoryItemId:guid}/stock-movements", async (Guid inventoryItemId, AdjustInventoryStockRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var command = new AdjustInventoryStockCommand(inventoryItemId, request.MovementType, request.Quantity, request.Reason);
                var result = await bus.InvokeAsync<Upshot<AdjustInventoryStockResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao movimentar estoque");
            })
            .WithName("AdjustInventoryStock")
            .WithTags("CatalogInventory")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Tecnico"))
            .Produces<AdjustInventoryStockResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}

/// <summary>
/// Request body for the stock-movement endpoint (the id travels in the route).
/// </summary>
public sealed record AdjustInventoryStockRequest(string MovementType, int Quantity, string? Reason);
