namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.ListCatalogedServices;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

/// <summary>
/// Maps the cataloged-services listing endpoint (AC: listagem e detalhamento - feature 03).
/// </summary>
public static class ListCatalogedServicesEndpoint
{
    public static IEndpointRouteBuilder MapListCatalogedServicesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/services", async (bool? onlyActive, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<IReadOnlyList<CatalogedServiceSummary>>(new ListCatalogedServicesQuery(onlyActive), cancellationToken).ConfigureAwait(false);
                return Results.Ok(result);
            })
            .WithName("ListCatalogedServices")
            .WithTags("CatalogInventory")
            .RequireAuthorization()
            .Produces<IReadOnlyList<CatalogedServiceSummary>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
