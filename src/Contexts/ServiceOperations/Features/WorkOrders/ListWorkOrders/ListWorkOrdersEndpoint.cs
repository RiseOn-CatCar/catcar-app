namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.ListWorkOrders;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the WorkOrder listing endpoint (feature 04: listagem básica de OS).
/// </summary>
public static class ListWorkOrdersEndpoint
{
    public static IEndpointRouteBuilder MapListWorkOrdersEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/work-orders", async (Guid? customerId, string? status, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<IReadOnlyList<WorkOrderSummary>>>(new ListWorkOrdersQuery(customerId, status), cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao listar OS");
            })
            .WithName("ListWorkOrders")
            .WithTags("ServiceOperations")
            .RequireAuthorization()
            .Produces<IReadOnlyList<WorkOrderSummary>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
