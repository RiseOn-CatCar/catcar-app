namespace CatCar.Contexts.ServiceOperations.Features.Customers.ListCustomers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

/// <summary>
/// Maps the customer listing endpoint (feature 04: CRUD de clientes).
/// </summary>
public static class ListCustomersEndpoint
{
    public static IEndpointRouteBuilder MapListCustomersEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/customers", async (bool? onlyActive, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<IReadOnlyList<CustomerSummary>>(new ListCustomersQuery(onlyActive), cancellationToken).ConfigureAwait(false);
                return Results.Ok(result);
            })
            .WithName("ListCustomers")
            .WithTags("ServiceOperations")
            .RequireAuthorization()
            .Produces<IReadOnlyList<CustomerSummary>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
