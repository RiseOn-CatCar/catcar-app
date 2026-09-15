using CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerByDocument;
using CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerById;
using CatCar.Contexts.ServiceOperations.Features.Customers.ListCustomers;
using CatCar.Contexts.ServiceOperations.Features.Customers.RegisterCustomer;
using CatCar.Contexts.ServiceOperations.Features.Customers.SetCustomerActiveStatus;
using CatCar.Contexts.ServiceOperations.Features.Customers.UpdateCustomer;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.GetVehicleById;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.ListVehiclesByCustomer;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.RegisterVehicle;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.SetVehicleActiveStatus;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.UpdateVehicle;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedPart;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedService;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.CompleteWorkOrder;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.DeliverWorkOrder;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderAverageExecutionTime;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderProgress;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderById;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.IssueBudget;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.ListWorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.StartDiagnosis;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.OpenWorkOrder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CatCar.Contexts.ServiceOperations;

/// <summary>
/// Endpoint route mapping for the ServiceOperations Bounded Context.
/// Maps vertical slice endpoints under the /api/v1/service-operations group.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps endpoints for the ServiceOperations Bounded Context.
    /// </summary>
    public static IEndpointRouteBuilder MapServiceOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // NOTE: `endpoints` is already the "/api/v1/service-operations" group created in Program.cs -
        // do NOT call MapGroup again here, it would double the route prefix.

        // Customers (feature 04)
        endpoints.MapRegisterCustomerEndpoint();
        endpoints.MapUpdateCustomerEndpoint();
        endpoints.MapSetCustomerActiveStatusEndpoint();
        endpoints.MapGetCustomerByIdEndpoint();
        endpoints.MapGetCustomerByDocumentEndpoint();
        endpoints.MapListCustomersEndpoint();

        // Vehicles (feature 04)
        endpoints.MapRegisterVehicleEndpoint();
        endpoints.MapUpdateVehicleEndpoint();
        endpoints.MapSetVehicleActiveStatusEndpoint();
        endpoints.MapGetVehicleByIdEndpoint();
        endpoints.MapListVehiclesByCustomerEndpoint();

        // WorkOrders (feature 04)
        endpoints.MapOpenWorkOrderEndpoint();
        endpoints.MapAddRequestedServiceEndpoint();
        endpoints.MapAddRequestedPartEndpoint();
        endpoints.MapIssueBudgetEndpoint();
        endpoints.MapGetWorkOrderByIdEndpoint();
        endpoints.MapListWorkOrdersEndpoint();
        endpoints.MapStartDiagnosisEndpoint();
        endpoints.MapCompleteWorkOrderEndpoint();
        endpoints.MapDeliverWorkOrderEndpoint();
        endpoints.MapGetWorkOrderProgressEndpoint();
        endpoints.MapGetWorkOrderAverageExecutionTimeEndpoint();

        return endpoints;
    }
}
