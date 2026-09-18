namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.OpenWorkOrder;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the WorkOrder opening endpoint (AC-001: abertura da OS - feature 04).
/// </summary>
public static class OpenWorkOrderEndpoint
{
    public static IEndpointRouteBuilder MapOpenWorkOrderEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/work-orders", async (OpenWorkOrderCommand command, IMessageBus bus, System.Security.Claims.ClaimsPrincipal user, CancellationToken cancellationToken) =>
            {
                if (user.HasClaim(c => c.Type == "role" && c.Value == "Customer"))
                {
                    var customerIdClaim = user.FindFirst("customer_id")?.Value
                        ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                    if (string.IsNullOrWhiteSpace(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var tokenCustomerId))
                    {
                        return Results.Problem(
                            detail: "Identificador do cliente inválido ou ausente no token.",
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "Não autorizado");
                    }

                    if (command.CustomerId != Guid.Empty && command.CustomerId != tokenCustomerId)
                    {
                        return Results.Problem(
                            detail: "Clientes só podem abrir ordens de serviço para si mesmos.",
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Acesso proibido");
                    }

                    if (command.CustomerId == Guid.Empty)
                    {
                        command = command with { CustomerId = tokenCustomerId };
                    }
                }

                var result = await bus.InvokeAsync<Upshot<OpenWorkOrderResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Created($"/api/v1/service-operations/work-orders/{result.Value.WorkOrderId}", result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao abrir OS");
            })
            .WithName("OpenWorkOrder")
            .WithTags("ServiceOperations")
            .RequireAuthorization(policy =>
            {
                policy.AddAuthenticationSchemes(
                    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                    "Customer");
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Administrador") ||
                    context.User.IsInRole("Tecnico") ||
                    context.User.IsInRole("Recepcionista") ||
                    context.User.HasClaim(c => c.Type == "role" && c.Value == "Customer"));
            })
            .Produces<OpenWorkOrderResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
