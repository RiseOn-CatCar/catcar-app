namespace CatCar.Contexts.CatalogInventory.Features.InventoryItems.RegisterInventoryItem;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiseOn.RailResult.Upshot;
using Wolverine;

/// <summary>
/// Maps the part/supply registration endpoint. Restricted to Administrador/Tecnico roles
/// (AC: CRUD de peças e insumos, com controle de estoque - feature 03).
/// </summary>
public static class RegisterInventoryItemEndpoint
{
    public static IEndpointRouteBuilder MapRegisterInventoryItemEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/inventory-items", async (RegisterInventoryItemCommand command, IMessageBus bus, CancellationToken cancellationToken) =>
            {
                var result = await bus.InvokeAsync<Upshot<RegisterInventoryItemResult>>(command, cancellationToken).ConfigureAwait(false);

                return result.IsSuccess
                    ? Results.Created($"/api/v1/catalog-inventory/inventory-items/{result.Value.Id}", result.Value)
                    : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Falha ao registrar peça/insumo");
            })
            .WithName("RegisterInventoryItem")
            .WithTags("CatalogInventory")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Tecnico"))
            .Produces<RegisterInventoryItemResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
