namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderProgress;

using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

public static class GetWorkOrderProgressEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkOrderProgressEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/work-orders/{id:guid}/progress", async (Guid id, HttpContext httpContext, IMessageBus bus, CancellationToken cancellationToken) =>
        {
            var customerIdValue = httpContext.User.FindFirst("customer_id")?.Value
                ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(customerIdValue, out var customerId))
            {
                return Results.Forbid();
            }

            var result = await bus.InvokeAsync<Upshot<WorkOrderProgress>>(new GetWorkOrderProgressQuery(id, customerId), cancellationToken).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error.Message, statusCode: StatusCodes.Status404NotFound);
        }).WithName("GetWorkOrderProgress").WithTags("ServiceOperations").RequireAuthorization("Customer");
        return endpoints;
    }
}
