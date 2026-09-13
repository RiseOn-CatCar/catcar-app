namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedPart;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the endpoint to include a requested part/supply on a WorkOrder (AC: inclusão de peças e
/// insumos necessários - feature 04).
/// </summary>
public static class AddRequestedPartEndpoint
{
    public static IEndpointRouteBuilder MapAddRequestedPartEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/work-orders/{workOrderId:guid}/parts", async (Guid workOrderId, AddRequestedPartRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var command = new AddRequestedPartCommand(workOrderId, request.InventoryItemId, request.Quantity);
                var result = await bus.InvokeAsync<Upshot<AddRequestedPartResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao incluir peça/insumo na OS");
            })
            .WithName("AddRequestedPart")
            .WithTags("ServiceOperations")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Tecnico"))
            .Produces<AddRequestedPartResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}

/// <summary>
/// Request body for including a requested part/supply (the WorkOrder id travels in the route).
/// </summary>
public sealed record AddRequestedPartRequest(Guid InventoryItemId, int Quantity);
