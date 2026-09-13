namespace CatCar.Contexts.ServiceOperations.Features.Vehicles.ListVehiclesByCustomer;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

/// <summary>
/// Maps the customer-vehicles listing endpoint (feature 04: CRUD de veículos).
/// </summary>
public static class ListVehiclesByCustomerEndpoint
{
    public static IEndpointRouteBuilder MapListVehiclesByCustomerEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/customers/{customerId:guid}/vehicles", async (Guid customerId, bool? onlyActive, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<IReadOnlyList<VehicleSummary>>(new ListVehiclesByCustomerQuery(customerId, onlyActive), cancellationToken).ConfigureAwait(false);
                return Results.Ok(result);
            })
            .WithName("ListVehiclesByCustomer")
            .WithTags("ServiceOperations")
            .RequireAuthorization()
            .Produces<IReadOnlyList<VehicleSummary>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
