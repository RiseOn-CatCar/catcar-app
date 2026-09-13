namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.UpdateVehicle;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the vehicle update endpoint (feature 04: CRUD de veículos).
/// </summary>
public static class UpdateVehicleEndpoint
{
    public static IEndpointRouteBuilder MapUpdateVehicleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/vehicles/{id:guid}", async (Guid id, UpdateVehicleRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var command = new UpdateVehicleCommand(id, request.Brand, request.Model, request.ManufactureYear);
                var result = await bus.InvokeAsync<Upshot<UpdateVehicleResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao atualizar veículo");
            })
            .WithName("UpdateVehicle")
            .WithTags("ServiceOperations")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Recepcionista"))
            .Produces<UpdateVehicleResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}

/// <summary>
/// Request body for updating a vehicle's brand/model/year.
/// </summary>
public sealed record UpdateVehicleRequest(string Brand, string Model, int ManufactureYear);
