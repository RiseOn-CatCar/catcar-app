namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderAverageExecutionTime;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

public static class GetWorkOrderAverageExecutionTimeEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkOrderAverageExecutionTimeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/work-orders/metrics/average-execution-time", async (IMessageBus bus, CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<WorkOrderAverageExecutionTime>(new GetWorkOrderAverageExecutionTimeQuery(), cancellationToken).ConfigureAwait(false)))
            .WithName("GetWorkOrderAverageExecutionTime").WithTags("ServiceOperations").RequireAuthorization();
        return endpoints;
    }
}
