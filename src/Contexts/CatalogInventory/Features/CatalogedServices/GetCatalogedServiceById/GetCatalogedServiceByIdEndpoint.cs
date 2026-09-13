namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.GetCatalogedServiceById;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the cataloged-service detail endpoint (AC: listagem e detalhamento - feature 03).
/// </summary>
public static class GetCatalogedServiceByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetCatalogedServiceByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/services/{serviceId:guid}", async (Guid serviceId, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<CatalogedServiceDetails>>(new GetCatalogedServiceByIdQuery(serviceId), cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status404NotFound, title: "Serviço não encontrado");
            })
            .WithName("GetCatalogedServiceById")
            .WithTags("CatalogInventory")
            .RequireAuthorization()
            .Produces<CatalogedServiceDetails>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
