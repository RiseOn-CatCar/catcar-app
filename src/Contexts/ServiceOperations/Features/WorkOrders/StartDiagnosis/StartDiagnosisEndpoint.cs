namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.StartDiagnosis;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

public static class StartDiagnosisEndpoint
{
    public static IEndpointRouteBuilder MapStartDiagnosisEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/work-orders/{id:guid}/start-diagnosis", async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
        {
            var result = await bus.InvokeAsync<Upshot<string>>(new StartDiagnosisCommand(id, Guid.NewGuid()), cancellationToken).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(new { status = result.Value }) : Results.Problem(result.Error.Message, statusCode: StatusCodes.Status400BadRequest);
        }).WithName("StartDiagnosis").WithTags("ServiceOperations").RequireAuthorization(policy => policy.RequireRole("Administrador", "Tecnico"));
        return endpoints;
    }
}
