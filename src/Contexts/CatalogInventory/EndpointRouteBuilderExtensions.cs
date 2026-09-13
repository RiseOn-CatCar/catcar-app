using CatCar.Contexts.CatalogInventory.Features.CatalogedServices.GetCatalogedServiceById;
using CatCar.Contexts.CatalogInventory.Features.CatalogedServices.ListCatalogedServices;
using CatCar.Contexts.CatalogInventory.Features.CatalogedServices.RegisterCatalogedService;
using CatCar.Contexts.CatalogInventory.Features.CatalogedServices.SetCatalogedServiceActiveStatus;
using CatCar.Contexts.CatalogInventory.Features.CatalogedServices.UpdateCatalogedService;
using CatCar.Contexts.CatalogInventory.Features.InventoryItems.AdjustInventoryStock;
using CatCar.Contexts.CatalogInventory.Features.InventoryItems.GetInventoryItemById;
using CatCar.Contexts.CatalogInventory.Features.InventoryItems.ListInventoryItems;
using CatCar.Contexts.CatalogInventory.Features.InventoryItems.RegisterInventoryItem;
using CatCar.Contexts.CatalogInventory.Features.InventoryItems.SetInventoryItemActiveStatus;
using CatCar.Contexts.CatalogInventory.Features.InventoryItems.UpdateInventoryItem;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CatCar.Contexts.CatalogInventory;

/// <summary>
/// Endpoint route mapping for the CatalogInventory Bounded Context.
/// Maps vertical slice endpoints under the /api/v1/catalog-inventory group.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps endpoints for the CatalogInventory Bounded Context.
    /// </summary>
    public static IEndpointRouteBuilder MapCatalogInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // NOTE: `endpoints` is already the "/api/v1/catalog-inventory" group created in Program.cs -
        // do NOT call MapGroup again here, it would double the route prefix.

        // Feature 03: CRUD de serviços.
        endpoints.MapRegisterCatalogedServiceEndpoint();
        endpoints.MapUpdateCatalogedServiceEndpoint();
        endpoints.MapSetCatalogedServiceActiveStatusEndpoint();
        endpoints.MapGetCatalogedServiceByIdEndpoint();
        endpoints.MapListCatalogedServicesEndpoint();

        // Feature 03: CRUD de peças e insumos, com controle de estoque.
        endpoints.MapRegisterInventoryItemEndpoint();
        endpoints.MapUpdateInventoryItemEndpoint();
        endpoints.MapAdjustInventoryStockEndpoint();
        endpoints.MapSetInventoryItemActiveStatusEndpoint();
        endpoints.MapGetInventoryItemByIdEndpoint();
        endpoints.MapListInventoryItemsEndpoint();

        return endpoints;
    }
}
