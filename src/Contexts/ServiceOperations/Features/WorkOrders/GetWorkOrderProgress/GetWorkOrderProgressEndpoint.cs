namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderProgress;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

public static class GetWorkOrderProgressEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkOrderProgressEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/work-orders/{id:guid}/progress", async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
        {
            var result = await bus.InvokeAsync<Upshot<WorkOrderProgress>>(new GetWorkOrderProgressQuery(id), cancellationToken).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error.Message, statusCode: StatusCodes.Status404NotFound);
        }).WithName("GetWorkOrderProgress").WithTags("ServiceOperations").RequireAuthorization();
        return endpoints;
    }
}
