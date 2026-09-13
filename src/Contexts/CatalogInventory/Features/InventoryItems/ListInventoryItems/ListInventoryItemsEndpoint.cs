namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.ListInventoryItems;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

/// <summary>
/// Maps the parts/supplies listing endpoint (AC: listagem e detalhamento, controle de estoque - feature 03).
/// </summary>
public static class ListInventoryItemsEndpoint
{
    public static IEndpointRouteBuilder MapListInventoryItemsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/inventory-items", async (bool? onlyActive, bool? onlyBelowMinimumStock, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<IReadOnlyList<InventoryItemSummary>>(new ListInventoryItemsQuery(onlyActive, onlyBelowMinimumStock), cancellationToken).ConfigureAwait(false);
                return Results.Ok(result);
            })
            .WithName("ListInventoryItems")
            .WithTags("CatalogInventory")
            .RequireAuthorization()
            .Produces<IReadOnlyList<InventoryItemSummary>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
