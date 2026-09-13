namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.RegisterVehicle;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the vehicle registration endpoint (AC: cadastro de veículo - feature 04).
/// </summary>
public static class RegisterVehicleEndpoint
{
    public static IEndpointRouteBuilder MapRegisterVehicleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/vehicles", async (RegisterVehicleCommand command, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<RegisterVehicleResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Created($"/api/v1/service-operations/vehicles/{result.Value.Id}", result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao registrar veículo");
            })
            .WithName("RegisterVehicle")
            .WithTags("ServiceOperations")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Recepcionista"))
            .Produces<RegisterVehicleResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
