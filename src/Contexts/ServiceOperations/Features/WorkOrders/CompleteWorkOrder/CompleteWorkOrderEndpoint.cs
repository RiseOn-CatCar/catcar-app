namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.CompleteWorkOrder;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

public static class CompleteWorkOrderEndpoint
{
    public static IEndpointRouteBuilder MapCompleteWorkOrderEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/work-orders/{id:guid}/complete", async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
        {
            var result = await bus.InvokeAsync<Upshot<string>>(new CompleteWorkOrderCommand(id, Guid.NewGuid()), cancellationToken).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(new { status = result.Value }) : Results.Problem(result.Error.Message, statusCode: StatusCodes.Status400BadRequest);
        }).WithName("CompleteWorkOrder").WithTags("ServiceOperations").RequireAuthorization(policy => policy.RequireRole("Administrador", "Tecnico"));
        return endpoints;
    }
}
