using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CatCar.Contexts.CatalogInventory;

/// <summary>
/// Endpoint route mapping for the CatalogInventory Bounded Context.
/// Maps vertical slice endpoints under the /api/v1/catalog-inventory group.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps endpoints for the CatalogInventory Bounded Context.
    /// </summary>
    public static IEndpointRouteBuilder MapCatalogInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/catalog-inventory");

        // Vertical slices will be mapped here as features are implemented.
        // Each feature folder contains Command/Query, Handler, Validator, and Endpoint files.

        return endpoints;
    }
}
