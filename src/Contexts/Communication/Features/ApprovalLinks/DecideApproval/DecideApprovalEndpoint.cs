namespace CatCar.Contexts.Communication.Features.ApprovalLinks.DecideApproval;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Request body for the public approval-decision endpoint.
/// </summary>
public sealed record DecideApprovalRequest(bool Approved, string? Reason);

/// <summary>
/// Maps the public, unauthenticated approve/reject endpoint. Security is provided by the single-use,
/// time-limited external token in the route - not JWT (AC: endpoint para aprova\u00e7\u00e3o ou recusa - feature 05).
/// </summary>
public static class DecideApprovalEndpoint
{
    public static IEndpointRouteBuilder MapDecideApprovalEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/approval-links/{rawToken}/decision", async (
                string rawToken, DecideApprovalRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var command = new DecideApprovalCommand(rawToken, request.Approved, request.Reason);
                var result = await bus.InvokeAsync<Upshot<DecideApprovalResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao registrar a decisão do orçamento");
            })
            .WithName("DecideApproval")
            .WithTags("Communication")
            .AllowAnonymous()
            .Produces<DecideApprovalResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
