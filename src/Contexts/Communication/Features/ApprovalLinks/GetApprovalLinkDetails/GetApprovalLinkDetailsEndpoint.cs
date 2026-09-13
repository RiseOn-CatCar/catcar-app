namespace CatCar.Contexts.Communication.Features.ApprovalLinks.GetApprovalLinkDetails;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the public, unauthenticated approval-link details endpoint. Security is provided by the
/// single-use, time-limited external token itself - not JWT (AC: "Auth cliente: Token externo de uso
/// único (sem JWT)" - feature 05).
/// </summary>
public static class GetApprovalLinkDetailsEndpoint
{
    public static IEndpointRouteBuilder MapGetApprovalLinkDetailsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/approval-links/{rawToken}", async (string rawToken, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<GetApprovalLinkDetailsResult>>(
                    new GetApprovalLinkDetailsQuery(rawToken), cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status404NotFound, title: "Link de aprovação inválido ou expirado");
            })
            .WithName("GetApprovalLinkDetails")
            .WithTags("Communication")
            .AllowAnonymous()
            .Produces<GetApprovalLinkDetailsResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
