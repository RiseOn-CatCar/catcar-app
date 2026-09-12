using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CatCar.Contexts.ServiceOperations;

/// <summary>
/// Endpoint route mapping for the ServiceOperations Bounded Context.
/// Maps vertical slice endpoints under the /api/v1/service-operations group.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps endpoints for the ServiceOperations Bounded Context.
    /// </summary>
    public static IEndpointRouteBuilder MapServiceOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // NOTE: `endpoints` is already the "/api/v1/service-operations" group created in Program.cs -
        // do NOT call MapGroup again here, it would double the route prefix.
        // Vertical slices will be mapped here as features are implemented.
        // Each feature folder contains Command/Query, Handler, Validator, and Endpoint files.

        return endpoints;
    }
}
