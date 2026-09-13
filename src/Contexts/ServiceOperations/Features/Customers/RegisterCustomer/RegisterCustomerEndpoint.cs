namespace CatCar.Contexts.ServiceOperations.Features.Customers.RegisterCustomer;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the customer registration endpoint. Restricted to Administrador/Recepcionista roles
/// (AC: identificação do cliente por CPF/CNPJ; CRUD de clientes - feature 04).
/// </summary>
public static class RegisterCustomerEndpoint
{
    public static IEndpointRouteBuilder MapRegisterCustomerEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/customers", async (RegisterCustomerCommand command, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<RegisterCustomerResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Created($"/api/v1/service-operations/customers/{result.Value.Id}", result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao registrar cliente");
            })
            .WithName("RegisterCustomer")
            .WithTags("ServiceOperations")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Recepcionista"))
            .Produces<RegisterCustomerResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
