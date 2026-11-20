namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.IssueBudget;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the budget-issuance endpoint (AC: orçamento gerado automaticamente com base nos serviços
/// e peças - feature 04).
/// </summary>
public static class IssueBudgetEndpoint
{
    public static IEndpointRouteBuilder MapIssueBudgetEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/work-orders/{workOrderId:guid}/budget", async (Guid workOrderId, HttpContext context, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var correlationId = context.Items.TryGetValue("CorrelationId", out var value) && value is Guid cid
                    ? cid
                    : Guid.TryParse(context.TraceIdentifier, out var parsedCorrelationId)
                        ? parsedCorrelationId
                        : Guid.NewGuid();
                var command = new IssueBudgetCommand(workOrderId, correlationId);
                var result = await bus.InvokeAsync<Upshot>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Accepted($"/api/v1/service-operations/work-orders/{workOrderId}")
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao gerar orçamento da OS");
            })
            .WithName("IssueBudget")
            .WithTags("ServiceOperations")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Tecnico"))
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
