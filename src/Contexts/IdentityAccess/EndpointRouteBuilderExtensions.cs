using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CatCar.Contexts.IdentityAccess;

/// <summary>
/// Endpoint route mapping for the IdentityAccess Bounded Context.
/// Maps vertical slice endpoints under the /api/v1/identity-access group.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps endpoints for the IdentityAccess Bounded Context.
    /// </summary>
    public static IEndpointRouteBuilder MapIdentityAccessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity-access");

        // Vertical slices will be mapped here as features are implemented.
        // Each feature folder contains Command/Query, Handler, Validator, and Endpoint files.

        return endpoints;
    }
}
