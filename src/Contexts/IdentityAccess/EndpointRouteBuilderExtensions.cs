using CatCar.Contexts.IdentityAccess.Features.AdministrativeUsers.RegisterAdministrativeUser;
using CatCar.Contexts.IdentityAccess.Features.Authentication.Login;
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
        // NOTE: `endpoints` is already the "/api/v1/identity-access" group created in Program.cs -
        // do NOT call MapGroup again here, it would double the route prefix.
        endpoints.MapLoginEndpoint();
        endpoints.MapRegisterAdministrativeUserEndpoint();

        return endpoints;
    }
}
