namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.GetInventoryItemById;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the part/supply detail endpoint (AC: listagem e detalhamento - feature 03).
/// </summary>
public static class GetInventoryItemByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetInventoryItemByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/inventory-items/{inventoryItemId:guid}", async (Guid inventoryItemId, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<InventoryItemDetails>>(new GetInventoryItemByIdQuery(inventoryItemId), cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status404NotFound, title: "Peça/insumo não encontrada");
            })
            .WithName("GetInventoryItemById")
            .WithTags("CatalogInventory")
            .RequireAuthorization()
            .Produces<InventoryItemDetails>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
