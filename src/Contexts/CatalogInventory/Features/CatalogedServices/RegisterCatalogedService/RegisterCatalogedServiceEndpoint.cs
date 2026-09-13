namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.RegisterCatalogedService;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the cataloged-service registration endpoint. Restricted to Administrador/Tecnico roles
/// (AC: CRUD de serviços - feature 03).
/// </summary>
public static class RegisterCatalogedServiceEndpoint
{
    public static IEndpointRouteBuilder MapRegisterCatalogedServiceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/services", async (RegisterCatalogedServiceCommand command, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<RegisterCatalogedServiceResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Created($"/api/v1/catalog-inventory/services/{result.Value.Id}", result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao registrar serviço");
            })
            .WithName("RegisterCatalogedService")
            .WithTags("CatalogInventory")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Tecnico"))
            .Produces<RegisterCatalogedServiceResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
