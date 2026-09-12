namespace CatCar.Contexts.IdentityAccess.Features.AdministrativeUsers.RegisterAdministrativeUser;

using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the administrative-user registration endpoint. Restricted to the Administrador role
/// (AC: proteção de endpoints - feature 02).
/// </summary>
public static class RegisterAdministrativeUserEndpoint
{
    public static IEndpointRouteBuilder MapRegisterAdministrativeUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/administrative-users", async (RegisterAdministrativeUserCommand command, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<RegisterAdministrativeUserResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Created($"/api/v1/identity-access/administrative-users/{result.Value.Id}", result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao registrar usuário administrativo");
            })
            .WithName("RegisterAdministrativeUser")
            .WithTags("IdentityAccess")
            .RequireAuthorization(policy => policy.RequireRole(nameof(AdministrativeRole.Administrador)))
            .Produces<RegisterAdministrativeUserResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
