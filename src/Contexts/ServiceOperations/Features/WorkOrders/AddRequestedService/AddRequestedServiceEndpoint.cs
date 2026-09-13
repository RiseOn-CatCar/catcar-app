namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedService;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the endpoint to include a requested service on a WorkOrder (AC: inclusão dos serviços
/// solicitados - feature 04).
/// </summary>
public static class AddRequestedServiceEndpoint
{
    public static IEndpointRouteBuilder MapAddRequestedServiceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/work-orders/{workOrderId:guid}/services", async (Guid workOrderId, AddRequestedServiceRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var command = new AddRequestedServiceCommand(workOrderId, request.CatalogedServiceId, request.Quantity);
                var result = await bus.InvokeAsync<Upshot<AddRequestedServiceResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao incluir serviço na OS");
            })
            .WithName("AddRequestedService")
            .WithTags("ServiceOperations")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Tecnico"))
            .Produces<AddRequestedServiceResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}

/// <summary>
/// Request body for including a requested service (the WorkOrder id travels in the route).
/// </summary>
public sealed record AddRequestedServiceRequest(Guid CatalogedServiceId, int Quantity);
