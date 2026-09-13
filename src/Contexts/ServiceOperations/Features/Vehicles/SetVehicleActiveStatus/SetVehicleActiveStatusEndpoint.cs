namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.SetVehicleActiveStatus;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the endpoint that activates/deactivates a vehicle (feature 04: CRUD de veículos).
/// </summary>
public static class SetVehicleActiveStatusEndpoint
{
    public static IEndpointRouteBuilder MapSetVehicleActiveStatusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/vehicles/{vehicleId:guid}/status", async (Guid vehicleId, SetVehicleActiveStatusRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var command = new SetVehicleActiveStatusCommand(vehicleId, request.IsActive);
                var result = await bus.InvokeAsync<Upshot<SetVehicleActiveStatusResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao alterar status do veículo");
            })
            .WithName("SetVehicleActiveStatus")
            .WithTags("ServiceOperations")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Recepcionista"))
            .Produces<SetVehicleActiveStatusResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}

/// <summary>
/// Request body for the status-change endpoint (the id travels in the route).
/// </summary>
public sealed record SetVehicleActiveStatusRequest(bool IsActive);
