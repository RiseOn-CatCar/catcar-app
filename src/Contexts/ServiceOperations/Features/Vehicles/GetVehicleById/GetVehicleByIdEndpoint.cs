namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.GetVehicleById;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the vehicle detail endpoint (feature 04: consulta de veículo por id).
/// </summary>
public static class GetVehicleByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetVehicleByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/vehicles/{vehicleId:guid}", async (Guid vehicleId, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<VehicleDetails>>(new GetVehicleByIdQuery(vehicleId), cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status404NotFound, title: "Veículo não encontrado");
            })
            .WithName("GetVehicleById")
            .WithTags("ServiceOperations")
            .RequireAuthorization()
            .Produces<VehicleDetails>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
