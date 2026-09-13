namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderById;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the WorkOrder detail endpoint (feature 04: consulta de OS por id).
/// </summary>
public static class GetWorkOrderByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkOrderByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/work-orders/{workOrderId:guid}", async (Guid workOrderId, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<WorkOrderDetails>>(new GetWorkOrderByIdQuery(workOrderId), cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status404NotFound, title: "OS não encontrada");
            })
            .WithName("GetWorkOrderById")
            .WithTags("ServiceOperations")
            .RequireAuthorization()
            .Produces<WorkOrderDetails>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
