using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CatCar.Contexts.Communication;

/// <summary>
/// Endpoint route mapping for the Communication Bounded Context.
/// Maps vertical slice endpoints under the /api/v1/communication group.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps endpoints for the Communication Bounded Context.
    /// </summary>
    public static IEndpointRouteBuilder MapCommunicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // NOTE: `endpoints` is already the "/api/v1/communication" group created in Program.cs -
        // do NOT call MapGroup again here, it would double the route prefix.
        // Vertical slices will be mapped here as features are implemented.
        // Each feature folder contains Command/Query, Handler, Validator, and Endpoint files.

        return endpoints;
    }
}
