namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedPart;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.AddRequestedService;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderAverageExecutionTime;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderById;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.GetWorkOrderProgress;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.ListWorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.OpenWorkOrder;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

[Collection(ApiIntegrationGroup.Name)]
public sealed class WorkOrderEndpointsTests(ApiFixture api)
{
    private const string WorkOrdersUrl = ApiFixture.ServiceOperationsPrefix + "/work-orders";
    private const string InitialDescription = "Strange noise coming from the engine.";

    // ---------------------------------------------------------------- open

    [Fact]
    public async Task OpenWorkOrder_AsAdministrator_ReturnsCreatedInReceivedStatus()
    {
        var (customerId, vehicleId) = await CreateCustomerAndVehicleAsync();

        using var response = await api.AdminClient.PostAsJsonAsync(WorkOrdersUrl, NewRequest(customerId, vehicleId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<OpenWorkOrderResult>();
        created.Should().NotBeNull();
        created!.CustomerId.Should().Be(customerId);
        created.VehicleId.Should().Be(vehicleId);
        created.Status.Should().Be(nameof(WorkOrderStatus.Received));
        created.InitialDescription.Should().Be(InitialDescription);
        response.Headers.Location!.ToString().Should().EndWith($"/work-orders/{created.WorkOrderId}");
    }

    [Fact]
    public async Task OpenWorkOrder_AsTechnicianWithCatalogItems_PersistsPricedLines()
    {
        var (customerId, vehicleId) = await CreateCustomerAndVehicleAsync();
        var request = NewRequest(
            customerId,
            vehicleId,
            services: [new OpenWorkOrderItemDto(FakeExecutionCatalog.ServiceId, 2)],
            parts: [new OpenWorkOrderItemDto(FakeExecutionCatalog.PartId, 3)]);

        using var response = await api.TechnicianClient.PostAsJsonAsync(WorkOrdersUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<OpenWorkOrderResult>();
        var details = await GetDetailsAsync(created!.WorkOrderId);
        details.RequestedServices.Should().ContainSingle()
            .Which.Should().Match<RequestedServiceLineDetails>(line =>
                line.CatalogedServiceId == FakeExecutionCatalog.ServiceId && line.Description == "Oil change" && line.UnitPrice == 150m && line.Quantity == 2 && line.LineTotal == 300m);
        details.RequestedParts.Should().ContainSingle()
            .Which.Should().Match<RequestedPartLineDetails>(line =>
                line.InventoryItemId == FakeExecutionCatalog.PartId && line.Description == "Oil filter" && line.UnitPrice == 40m && line.Quantity == 3 && line.LineTotal == 120m);
    }

    [Fact]
    public async Task OpenWorkOrder_AsCustomerWithoutCustomerId_OpensForTheTokenCustomer()
    {
        var (customerId, vehicleId) = await CreateCustomerAndVehicleAsync();

        using var response = await SendAsync(HttpMethod.Post, WorkOrdersUrl, CreateCustomerToken(customerId), NewRequest(Guid.Empty, vehicleId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<OpenWorkOrderResult>();
        created!.CustomerId.Should().Be(customerId);
        created.Status.Should().Be(nameof(WorkOrderStatus.Received));
    }

    [Fact]
    public async Task OpenWorkOrder_AsCustomerForAnotherCustomer_ReturnsForbidden()
    {
        var (customerId, vehicleId) = await CreateCustomerAndVehicleAsync();

        using var response = await SendAsync(HttpMethod.Post, WorkOrdersUrl, CreateCustomerToken(Guid.NewGuid()), NewRequest(customerId, vehicleId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OpenWorkOrder_AsCustomerWithoutCustomerIdClaim_ReturnsUnauthorized()
    {
        var (customerId, vehicleId) = await CreateCustomerAndVehicleAsync();

        using var response = await SendAsync(HttpMethod.Post, WorkOrdersUrl, CreateCustomerToken(customerId: null), NewRequest(customerId, vehicleId));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OpenWorkOrder_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.PostAsJsonAsync(WorkOrdersUrl, NewRequest(Guid.NewGuid(), Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OpenWorkOrder_ForUnknownCustomer_ReturnsBadRequest()
    {
        using var response = await api.AdminClient.PostAsJsonAsync(WorkOrdersUrl, NewRequest(Guid.NewGuid(), Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OpenWorkOrder_ForUnknownVehicle_ReturnsBadRequest()
    {
        var customerId = await api.CreateCustomerAsync();

        using var response = await api.AdminClient.PostAsJsonAsync(WorkOrdersUrl, NewRequest(customerId, Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OpenWorkOrder_WithVehicleOfAnotherCustomer_ReturnsBadRequest()
    {
        var customerId = await api.CreateCustomerAsync();
        var otherCustomerId = await api.CreateCustomerAsync();
        var otherVehicleId = await api.CreateVehicleAsync(otherCustomerId);

        using var response = await api.AdminClient.PostAsJsonAsync(WorkOrdersUrl, NewRequest(customerId, otherVehicleId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OpenWorkOrder_WithoutDescription_ReturnsBadRequest(string description)
    {
        var (customerId, vehicleId) = await CreateCustomerAndVehicleAsync();

        using var response = await api.AdminClient.PostAsJsonAsync(WorkOrdersUrl, NewRequest(customerId, vehicleId, description));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OpenWorkOrder_WithTooLongDescription_ReturnsBadRequest()
    {
        var (customerId, vehicleId) = await CreateCustomerAndVehicleAsync();

        using var response = await api.AdminClient.PostAsJsonAsync(WorkOrdersUrl, NewRequest(customerId, vehicleId, new string('x', 1001)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OpenWorkOrder_WithInactiveCatalogService_ReturnsBadRequestAndDoesNotPersist()
    {
        var (customerId, vehicleId) = await CreateCustomerAndVehicleAsync();
        var request = NewRequest(customerId, vehicleId, services: [new OpenWorkOrderItemDto(FakeExecutionCatalog.InactiveServiceId, 1)]);

        using var response = await api.AdminClient.PostAsJsonAsync(WorkOrdersUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ListAsync($"?customerId={customerId}")).Should().BeEmpty();
    }

    [Fact]
    public async Task OpenWorkOrder_WithUnknownCatalogPart_ReturnsBadRequest()
    {
        var (customerId, vehicleId) = await CreateCustomerAndVehicleAsync();
        var request = NewRequest(customerId, vehicleId, parts: [new OpenWorkOrderItemDto(Guid.NewGuid(), 1)]);

        using var response = await api.AdminClient.PostAsJsonAsync(WorkOrdersUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OpenWorkOrder_WithInvalidQuantity_ReturnsBadRequest()
    {
        var (customerId, vehicleId) = await CreateCustomerAndVehicleAsync();
        var request = NewRequest(customerId, vehicleId, services: [new OpenWorkOrderItemDto(FakeExecutionCatalog.ServiceId, 0)]);

        using var response = await api.AdminClient.PostAsJsonAsync(WorkOrdersUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------------------------------------------------------------- read

    [Fact]
    public async Task GetWorkOrderById_ForExistingOrder_ReturnsDetails()
    {
        var (customerId, vehicleId, workOrderId) = await OpenWorkOrderAsync();

        var details = await GetDetailsAsync(workOrderId);

        details.Id.Should().Be(workOrderId);
        details.CustomerId.Should().Be(customerId);
        details.VehicleId.Should().Be(vehicleId);
        details.Status.Should().Be(nameof(WorkOrderStatus.Received));
        details.InitialDescription.Should().Be(InitialDescription);
        details.RequestedServices.Should().BeEmpty();
        details.RequestedParts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWorkOrderById_ForUnknownOrder_ReturnsNotFound()
    {
        using var response = await api.AdminClient.GetAsync($"{WorkOrdersUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWorkOrderById_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.GetAsync($"{WorkOrdersUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListWorkOrders_FilteredByCustomer_ReturnsOnlyThatCustomersOrders()
    {
        var (customerId, _, firstId) = await OpenWorkOrderAsync();
        await OpenWorkOrderAsync();

        var orders = await ListAsync($"?customerId={customerId}");

        orders.Should().ContainSingle().Which.Should().Match<WorkOrderSummary>(order =>
            order.Id == firstId && order.CustomerId == customerId && order.Status == nameof(WorkOrderStatus.Received));
    }

    [Fact]
    public async Task ListWorkOrders_FilteredByStatus_ReturnsOnlyMatchingOrders()
    {
        var (customerId, _, receivedId) = await OpenWorkOrderAsync();
        var vehicleId = await api.CreateVehicleAsync(customerId);
        var diagnosisId = await OpenAsync(customerId, vehicleId);
        (await PostAsync($"{WorkOrdersUrl}/{diagnosisId}/start-diagnosis")).StatusCode.Should().Be(HttpStatusCode.OK);

        var inDiagnosis = await ListAsync($"?customerId={customerId}&status=indiagnosis");
        var received = await ListAsync($"?customerId={customerId}&status={nameof(WorkOrderStatus.Received)}");

        inDiagnosis.Should().ContainSingle().Which.Id.Should().Be(diagnosisId);
        received.Should().ContainSingle().Which.Id.Should().Be(receivedId);
    }

    [Fact]
    public async Task ListWorkOrders_WithInvalidStatus_ReturnsBadRequest()
    {
        using var response = await api.AdminClient.GetAsync($"{WorkOrdersUrl}?status=not-a-status");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListWorkOrders_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.GetAsync(WorkOrdersUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAverageExecutionTime_AsAdministrator_ReturnsMetrics()
    {
        using var response = await api.AdminClient.GetAsync($"{WorkOrdersUrl}/metrics/average-execution-time");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var metrics = await response.Content.ReadFromJsonAsync<WorkOrderAverageExecutionTime>();
        metrics.Should().NotBeNull();
        metrics!.TotalCompletedWorkOrders.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetAverageExecutionTime_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.GetAsync($"{WorkOrdersUrl}/metrics/average-execution-time");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------- lines

    [Fact]
    public async Task AddRequestedService_WithActiveService_ReturnsPricedLine()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();

        using var response = await api.AdminClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{workOrderId}/services", new AddRequestedServiceRequest(FakeExecutionCatalog.ServiceId, 2));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AddRequestedServiceResult>();
        result.Should().Be(new AddRequestedServiceResult(workOrderId, FakeExecutionCatalog.ServiceId, "Oil change", 150m, 2, 300m));
        (await GetDetailsAsync(workOrderId)).RequestedServices.Should().ContainSingle();
    }

    [Fact]
    public async Task AddRequestedService_AsTechnician_IsAllowed()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();

        using var response = await api.TechnicianClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{workOrderId}/services", new AddRequestedServiceRequest(FakeExecutionCatalog.ServiceId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddRequestedService_AsReceptionist_ReturnsForbidden()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();

        using var response = await api.ReceptionistClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{workOrderId}/services", new AddRequestedServiceRequest(FakeExecutionCatalog.ServiceId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddRequestedService_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{Guid.NewGuid()}/services", new AddRequestedServiceRequest(FakeExecutionCatalog.ServiceId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddRequestedService_ForUnknownOrder_ReturnsBadRequest()
    {
        using var response = await api.AdminClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{Guid.NewGuid()}/services", new AddRequestedServiceRequest(FakeExecutionCatalog.ServiceId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddRequestedService_WithInvalidQuantity_ReturnsBadRequest(int quantity)
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();

        using var response = await api.AdminClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{workOrderId}/services", new AddRequestedServiceRequest(FakeExecutionCatalog.ServiceId, quantity));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetDetailsAsync(workOrderId)).RequestedServices.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddRequestedService_WithInactiveOrUnknownService_ReturnsBadRequest(bool unknown)
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();
        var serviceId = unknown ? Guid.NewGuid() : FakeExecutionCatalog.InactiveServiceId;

        using var response = await api.AdminClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{workOrderId}/services", new AddRequestedServiceRequest(serviceId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetDetailsAsync(workOrderId)).RequestedServices.Should().BeEmpty();
    }

    [Fact]
    public async Task AddRequestedService_AfterCompletion_ReturnsBadRequest()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();
        await AddServiceAsync(workOrderId);
        await AdvanceToCompletedAsync(workOrderId);

        using var response = await api.AdminClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{workOrderId}/services", new AddRequestedServiceRequest(FakeExecutionCatalog.ServiceId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddRequestedPart_WithSufficientStock_ReturnsPricedLine()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();

        using var response = await api.AdminClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{workOrderId}/parts", new AddRequestedPartRequest(FakeExecutionCatalog.PartId, 4));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AddRequestedPartResult>();
        result.Should().Be(new AddRequestedPartResult(workOrderId, FakeExecutionCatalog.PartId, "Oil filter", 40m, 4, 160m));
        (await GetDetailsAsync(workOrderId)).RequestedParts.Should().ContainSingle();
    }

    [Fact]
    public async Task AddRequestedPart_WithInsufficientStock_ReturnsBadRequest()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();
        var partId = Guid.NewGuid();
        api.Catalog.AddPart(partId, "Scarce part", 10m, quantityInStock: 2);

        using var response = await api.AdminClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{workOrderId}/parts", new AddRequestedPartRequest(partId, 3));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetDetailsAsync(workOrderId)).RequestedParts.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddRequestedPart_WithInactiveOrUnknownPart_ReturnsBadRequest(bool unknown)
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();
        var partId = unknown ? Guid.NewGuid() : FakeExecutionCatalog.InactivePartId;

        using var response = await api.AdminClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{workOrderId}/parts", new AddRequestedPartRequest(partId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddRequestedPart_WithZeroQuantity_ReturnsBadRequest()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();

        using var response = await api.AdminClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{workOrderId}/parts", new AddRequestedPartRequest(FakeExecutionCatalog.PartId, 0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddRequestedPart_ForUnknownOrder_ReturnsBadRequest()
    {
        using var response = await api.AdminClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{Guid.NewGuid()}/parts", new AddRequestedPartRequest(FakeExecutionCatalog.PartId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddRequestedPart_AsReceptionist_ReturnsForbidden()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();

        using var response = await api.ReceptionistClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{workOrderId}/parts", new AddRequestedPartRequest(FakeExecutionCatalog.PartId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddRequestedPart_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{Guid.NewGuid()}/parts", new AddRequestedPartRequest(FakeExecutionCatalog.PartId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------- diagnosis

    [Fact]
    public async Task StartDiagnosis_ForReceivedOrder_MovesToInDiagnosis()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();

        using var response = await PostAsync($"{WorkOrdersUrl}/{workOrderId}/start-diagnosis");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<StatusResponse>())!.Status.Should().Be(nameof(WorkOrderStatus.InDiagnosis));
        (await GetDetailsAsync(workOrderId)).Status.Should().Be(nameof(WorkOrderStatus.InDiagnosis));
    }

    [Fact]
    public async Task StartDiagnosis_Twice_ReturnsBadRequest()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();
        (await PostAsync($"{WorkOrdersUrl}/{workOrderId}/start-diagnosis")).StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await PostAsync($"{WorkOrdersUrl}/{workOrderId}/start-diagnosis");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StartDiagnosis_ForUnknownOrder_ReturnsBadRequest()
    {
        using var response = await PostAsync($"{WorkOrdersUrl}/{Guid.NewGuid()}/start-diagnosis");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StartDiagnosis_AsReceptionist_ReturnsForbidden()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();

        using var response = await api.ReceptionistClient.PostAsync($"{WorkOrdersUrl}/{workOrderId}/start-diagnosis", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StartDiagnosis_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.PostAsync($"{WorkOrdersUrl}/{Guid.NewGuid()}/start-diagnosis", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------- budget

    [Fact]
    public async Task IssueBudget_InDiagnosisWithLines_ReturnsAccepted()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();
        await AddServiceAsync(workOrderId);
        (await PostAsync($"{WorkOrdersUrl}/{workOrderId}/start-diagnosis")).StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await PostAsync($"{WorkOrdersUrl}/{workOrderId}/budget");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response.Headers.Location!.ToString().Should().EndWith($"/work-orders/{workOrderId}");
    }

    [Fact]
    public async Task IssueBudget_WhileARequestIsInProgress_ReturnsBadRequest()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();
        await AddServiceAsync(workOrderId);
        (await PostAsync($"{WorkOrdersUrl}/{workOrderId}/start-diagnosis")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await PostAsync($"{WorkOrdersUrl}/{workOrderId}/budget")).StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var response = await PostAsync($"{WorkOrdersUrl}/{workOrderId}/budget");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IssueBudget_WithoutLines_ReturnsBadRequest()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();
        (await PostAsync($"{WorkOrdersUrl}/{workOrderId}/start-diagnosis")).StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await PostAsync($"{WorkOrdersUrl}/{workOrderId}/budget");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IssueBudget_BeforeDiagnosis_ReturnsBadRequest()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();
        await AddServiceAsync(workOrderId);

        using var response = await PostAsync($"{WorkOrdersUrl}/{workOrderId}/budget");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IssueBudget_ForCustomerWithoutEmail_ReturnsBadRequest()
    {
        var customerId = await api.CreateCustomerAsync(email: null);
        var vehicleId = await api.CreateVehicleAsync(customerId);
        var workOrderId = await OpenAsync(customerId, vehicleId);
        await AddServiceAsync(workOrderId);
        (await PostAsync($"{WorkOrdersUrl}/{workOrderId}/start-diagnosis")).StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await PostAsync($"{WorkOrdersUrl}/{workOrderId}/budget");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IssueBudget_ForUnknownOrder_ReturnsBadRequest()
    {
        using var response = await PostAsync($"{WorkOrdersUrl}/{Guid.NewGuid()}/budget");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IssueBudget_AsReceptionist_ReturnsForbidden()
    {
        using var response = await api.ReceptionistClient.PostAsync($"{WorkOrdersUrl}/{Guid.NewGuid()}/budget", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task IssueBudget_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.PostAsync($"{WorkOrdersUrl}/{Guid.NewGuid()}/budget", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------- delivery

    [Fact]
    public async Task DeliverWorkOrder_ForCompletedOrder_MovesToDelivered()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();
        await AddServiceAsync(workOrderId);
        await AdvanceToCompletedAsync(workOrderId);

        using var response = await PostAsync($"{WorkOrdersUrl}/{workOrderId}/deliver");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<StatusResponse>())!.Status.Should().Be(nameof(WorkOrderStatus.Delivered));
        (await GetDetailsAsync(workOrderId)).Status.Should().Be(nameof(WorkOrderStatus.Delivered));
    }

    [Fact]
    public async Task DeliverWorkOrder_Twice_ReturnsBadRequest()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();
        await AddServiceAsync(workOrderId);
        await AdvanceToCompletedAsync(workOrderId);
        (await PostAsync($"{WorkOrdersUrl}/{workOrderId}/deliver")).StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await PostAsync($"{WorkOrdersUrl}/{workOrderId}/deliver");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeliverWorkOrder_BeforeCompletion_ReturnsBadRequest()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();

        using var response = await PostAsync($"{WorkOrdersUrl}/{workOrderId}/deliver");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetDetailsAsync(workOrderId)).Status.Should().Be(nameof(WorkOrderStatus.Received));
    }

    [Fact]
    public async Task DeliverWorkOrder_ForUnknownOrder_ReturnsBadRequest()
    {
        using var response = await PostAsync($"{WorkOrdersUrl}/{Guid.NewGuid()}/deliver");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeliverWorkOrder_AsReceptionist_ReturnsForbidden()
    {
        using var response = await api.ReceptionistClient.PostAsync($"{WorkOrdersUrl}/{Guid.NewGuid()}/deliver", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeliverWorkOrder_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.PostAsync($"{WorkOrdersUrl}/{Guid.NewGuid()}/deliver", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------- progress

    [Fact]
    public async Task GetWorkOrderProgress_AsOwnerCustomer_ReturnsTimeline()
    {
        var (customerId, _, workOrderId) = await OpenWorkOrderAsync();
        (await PostAsync($"{WorkOrdersUrl}/{workOrderId}/start-diagnosis")).StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await SendAsync(HttpMethod.Get, $"{WorkOrdersUrl}/{workOrderId}/progress", CreateCustomerToken(customerId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var progress = await response.Content.ReadFromJsonAsync<WorkOrderProgress>();
        progress!.WorkOrderId.Should().Be(workOrderId);
        progress.Status.Should().Be(nameof(WorkOrderStatus.InDiagnosis));
        progress.Timeline.Single(stage => stage.Status == nameof(WorkOrderStatus.Received)).Reached.Should().BeTrue();
        progress.Timeline.Single(stage => stage.Status == nameof(WorkOrderStatus.InDiagnosis)).Reached.Should().BeTrue();
        progress.Timeline.Single(stage => stage.Status == nameof(WorkOrderStatus.Delivered)).Reached.Should().BeFalse();
    }

    [Fact]
    public async Task GetWorkOrderProgress_AsAnotherCustomer_ReturnsNotFound()
    {
        var (_, _, workOrderId) = await OpenWorkOrderAsync();

        using var response = await SendAsync(HttpMethod.Get, $"{WorkOrdersUrl}/{workOrderId}/progress", CreateCustomerToken(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWorkOrderProgress_ForUnknownOrder_ReturnsNotFound()
    {
        using var response = await SendAsync(HttpMethod.Get, $"{WorkOrdersUrl}/{Guid.NewGuid()}/progress", CreateCustomerToken(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWorkOrderProgress_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.GetAsync($"{WorkOrdersUrl}/{Guid.NewGuid()}/progress");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWorkOrderProgress_WithAdministrativeToken_IsRejected()
    {
        using var response = await api.AdminClient.GetAsync($"{WorkOrdersUrl}/{Guid.NewGuid()}/progress");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    // ---------------------------------------------------------------- helpers

    private static OpenWorkOrderRequest NewRequest(
        Guid customerId,
        Guid vehicleId,
        string description = InitialDescription,
        IReadOnlyList<OpenWorkOrderItemDto>? services = null,
        IReadOnlyList<OpenWorkOrderItemDto>? parts = null)
        => new(customerId, vehicleId, description, services, parts);

    private static string CreateCustomerToken(Guid? customerId)
    {
        var claims = new List<Claim> { new("role", "Customer") };
        if (customerId is { } id)
            claims.Add(new Claim("customer_id", id.ToString()));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiFixture.JwtSecret)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("CatCar", "CatCar.Api", claims, expires: DateTime.UtcNow.AddMinutes(30), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        return await api.Client.SendAsync(request);
    }

    private Task<HttpResponseMessage> PostAsync(string url) => api.AdminClient.PostAsync(url, content: null);

    private async Task<(Guid CustomerId, Guid VehicleId)> CreateCustomerAndVehicleAsync()
    {
        var customerId = await api.CreateCustomerAsync();
        return (customerId, await api.CreateVehicleAsync(customerId));
    }

    private async Task<Guid> OpenAsync(Guid customerId, Guid vehicleId)
    {
        using var response = await api.AdminClient.PostAsJsonAsync(WorkOrdersUrl, NewRequest(customerId, vehicleId));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<OpenWorkOrderResult>();
        return created!.WorkOrderId;
    }

    private async Task<(Guid CustomerId, Guid VehicleId, Guid WorkOrderId)> OpenWorkOrderAsync()
    {
        var (customerId, vehicleId) = await CreateCustomerAndVehicleAsync();
        return (customerId, vehicleId, await OpenAsync(customerId, vehicleId));
    }

    private async Task AddServiceAsync(Guid workOrderId)
    {
        using var response = await api.AdminClient.PostAsJsonAsync(
            $"{WorkOrdersUrl}/{workOrderId}/services", new AddRequestedServiceRequest(FakeExecutionCatalog.ServiceId, 1));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<WorkOrderDetails> GetDetailsAsync(Guid workOrderId)
    {
        using var response = await api.AdminClient.GetAsync($"{WorkOrdersUrl}/{workOrderId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<WorkOrderDetails>())!;
    }

    private async Task<IReadOnlyList<WorkOrderSummary>> ListAsync(string query)
    {
        using var response = await api.AdminClient.GetAsync($"{WorkOrdersUrl}{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<WorkOrderSummary>>())!;
    }

    /// <summary>
    /// Drives an order to <see cref="WorkOrderStatus.Completed"/> through the aggregate, standing in for the
    /// saga messages that the stubbed billing and execution services would send.
    /// </summary>
    private async Task AdvanceToCompletedAsync(Guid workOrderId)
    {
        (await PostAsync($"{WorkOrdersUrl}/{workOrderId}/start-diagnosis")).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = api.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkOrderRepository>();
        var workOrder = await repository.GetByIdAsync(workOrderId);
        workOrder.Should().NotBeNull();

        var now = DateTime.UtcNow;
        workOrder!.MarkBudgetIssued(Guid.NewGuid(), now).IsSuccess.Should().BeTrue();
        workOrder.RecordApproval(true, DateTime.UtcNow).IsSuccess.Should().BeTrue();
        workOrder.AwaitPayment(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        workOrder.StartExecution(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        workOrder.Complete(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        await repository.SaveChangesAsync();
    }

    private sealed record StatusResponse(string Status);
}
