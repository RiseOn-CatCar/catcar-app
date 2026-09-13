namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.OpenWorkOrder;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the WorkOrder opening endpoint (AC-001: abertura da OS - feature 04).
/// </summary>
public static class OpenWorkOrderEndpoint
{
    public static IEndpointRouteBuilder MapOpenWorkOrderEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/work-orders", async (OpenWorkOrderCommand command, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<OpenWorkOrderResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Created($"/api/v1/service-operations/work-orders/{result.Value.Id}", result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao abrir OS");
            })
            .WithName("OpenWorkOrder")
            .WithTags("ServiceOperations")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Tecnico"))
            .Produces<OpenWorkOrderResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
