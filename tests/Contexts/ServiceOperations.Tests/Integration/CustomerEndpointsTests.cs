namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerByDocument;
using CatCar.Contexts.ServiceOperations.Features.Customers.GetCustomerById;
using CatCar.Contexts.ServiceOperations.Features.Customers.ListCustomers;
using CatCar.Contexts.ServiceOperations.Features.Customers.RegisterCustomer;
using CatCar.Contexts.ServiceOperations.Features.Customers.SetCustomerActiveStatus;
using CatCar.Contexts.ServiceOperations.Features.Customers.UpdateCustomer;
using FluentAssertions;
using Xunit;

[Collection(ApiIntegrationGroup.Name)]
public sealed class CustomerEndpointsTests(ApiFixture api)
{
    private const string CustomersUrl = ApiFixture.ServiceOperationsPrefix + "/customers";

    [Fact]
    public async Task RegisterCustomer_AsAdministrator_ReturnsCreatedAndLocation()
    {
        var document = ApiFixture.NewCpf();

        using var response = await api.AdminClient.PostAsJsonAsync(CustomersUrl, new RegisterCustomerCommand(document, "Maria Silva", "85999990000", "maria@example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<RegisterCustomerResult>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Maria Silva");
        created.Email.Should().Be("maria@example.com");
        created.IsActive.Should().BeTrue();
        response.Headers.Location!.ToString().Should().EndWith($"/customers/{created.Id}");
    }

    [Fact]
    public async Task RegisterCustomer_AsReceptionist_ReturnsCreated()
    {
        using var response = await api.ReceptionistClient.PostAsJsonAsync(CustomersUrl, new RegisterCustomerCommand(ApiFixture.NewCpf(), "Front Desk Customer", "85999990000", null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RegisterCustomer_AsTechnician_ReturnsForbidden()
    {
        using var response = await api.TechnicianClient.PostAsJsonAsync(CustomersUrl, new RegisterCustomerCommand(ApiFixture.NewCpf(), "Nope", "85999990000", null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RegisterCustomer_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.PostAsJsonAsync(CustomersUrl, new RegisterCustomerCommand(ApiFixture.NewCpf(), "Nope", "85999990000", null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterCustomer_WithDuplicatedDocument_ReturnsBadRequest()
    {
        var document = ApiFixture.NewCpf();
        using var first = await api.AdminClient.PostAsJsonAsync(CustomersUrl, new RegisterCustomerCommand(document, "First", "85999990000", null));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        using var second = await api.AdminClient.PostAsJsonAsync(CustomersUrl, new RegisterCustomerCommand(document, "Second", "85999990000", null));

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("123", "Valid Name", "85999990000")]
    [InlineData("11111111111", "Valid Name", "85999990000")]
    [InlineData("", "Valid Name", "85999990000")]
    public async Task RegisterCustomer_WithInvalidDocument_ReturnsBadRequest(string document, string name, string phone)
    {
        using var response = await api.AdminClient.PostAsJsonAsync(CustomersUrl, new RegisterCustomerCommand(document, name, phone, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterCustomer_WithoutName_ReturnsBadRequest()
    {
        using var response = await api.AdminClient.PostAsJsonAsync(CustomersUrl, new RegisterCustomerCommand(ApiFixture.NewCpf(), string.Empty, "85999990000", null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCustomerById_ForExistingCustomer_ReturnsDetails()
    {
        var id = await api.CreateCustomerAsync(name: "Details Customer");

        using var response = await api.ReceptionistClient.GetAsync($"{CustomersUrl}/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<CustomerDetails>();
        details!.Id.Should().Be(id);
        details.Name.Should().Be("Details Customer");
    }

    [Fact]
    public async Task GetCustomerById_ForUnknownCustomer_ReturnsNotFound()
    {
        using var response = await api.AdminClient.GetAsync($"{CustomersUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCustomerById_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.GetAsync($"{CustomersUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCustomerByDocument_ForExistingCustomer_ReturnsDetails()
    {
        var document = ApiFixture.NewCpf();
        using var created = await api.AdminClient.PostAsJsonAsync(CustomersUrl, new RegisterCustomerCommand(document, "By Document", "85999990000", null));
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await created.Content.ReadFromJsonAsync<RegisterCustomerResult>();

        using var response = await api.AdminClient.GetAsync($"{CustomersUrl}/by-document/{document}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<CustomerByDocumentDetails>();
        details!.Id.Should().Be(customer!.Id);
    }

    [Fact]
    public async Task GetCustomerByDocument_ForUnknownDocument_ReturnsNotFound()
    {
        using var response = await api.AdminClient.GetAsync($"{CustomersUrl}/by-document/{ApiFixture.NewCpf()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListCustomers_ReturnsRegisteredCustomersAndHonorsActiveFilter()
    {
        var activeId = await api.CreateCustomerAsync();
        var inactiveId = await api.CreateCustomerAsync();
        using var deactivate = await api.AdminClient.PatchAsJsonAsync($"{CustomersUrl}/{inactiveId}/status", new SetCustomerActiveStatusRequest(false));
        deactivate.StatusCode.Should().Be(HttpStatusCode.OK);

        var all = await api.AdminClient.GetFromJsonAsync<List<CustomerSummary>>(CustomersUrl);
        var onlyActive = await api.AdminClient.GetFromJsonAsync<List<CustomerSummary>>($"{CustomersUrl}?onlyActive=true");

        all.Should().Contain(c => c.Id == activeId).And.Contain(c => c.Id == inactiveId);
        onlyActive.Should().Contain(c => c.Id == activeId).And.NotContain(c => c.Id == inactiveId);
    }

    [Fact]
    public async Task UpdateCustomer_WithValidData_ReturnsUpdatedCustomer()
    {
        var id = await api.CreateCustomerAsync();

        using var response = await api.ReceptionistClient.PutAsJsonAsync($"{CustomersUrl}/{id}", new UpdateCustomerRequest("Renamed Customer", "85988887777", "renamed@example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<UpdateCustomerResult>();
        updated!.Name.Should().Be("Renamed Customer");
        updated.Phone.Should().Be("85988887777");
        updated.Email.Should().Be("renamed@example.com");

        var persisted = await api.AdminClient.GetFromJsonAsync<CustomerDetails>($"{CustomersUrl}/{id}");
        persisted!.Name.Should().Be("Renamed Customer");
    }

    [Fact]
    public async Task UpdateCustomer_ForUnknownCustomer_ReturnsBadRequest()
    {
        using var response = await api.AdminClient.PutAsJsonAsync($"{CustomersUrl}/{Guid.NewGuid()}", new UpdateCustomerRequest("Name", "85988887777", null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCustomer_WithoutName_ReturnsBadRequest()
    {
        var id = await api.CreateCustomerAsync();

        using var response = await api.AdminClient.PutAsJsonAsync($"{CustomersUrl}/{id}", new UpdateCustomerRequest(string.Empty, "85988887777", null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCustomer_AsTechnician_ReturnsForbidden()
    {
        var id = await api.CreateCustomerAsync();

        using var response = await api.TechnicianClient.PutAsJsonAsync($"{CustomersUrl}/{id}", new UpdateCustomerRequest("Name", "85988887777", null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetCustomerActiveStatus_TogglesCustomerStatus()
    {
        var id = await api.CreateCustomerAsync();

        using var deactivated = await api.AdminClient.PatchAsJsonAsync($"{CustomersUrl}/{id}/status", new SetCustomerActiveStatusRequest(false));
        var afterDeactivation = await api.AdminClient.GetFromJsonAsync<CustomerDetails>($"{CustomersUrl}/{id}");
        using var reactivated = await api.AdminClient.PatchAsJsonAsync($"{CustomersUrl}/{id}/status", new SetCustomerActiveStatusRequest(true));
        var afterReactivation = await api.AdminClient.GetFromJsonAsync<CustomerDetails>($"{CustomersUrl}/{id}");

        deactivated.StatusCode.Should().Be(HttpStatusCode.OK);
        reactivated.StatusCode.Should().Be(HttpStatusCode.OK);
        afterDeactivation!.IsActive.Should().BeFalse();
        afterReactivation!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SetCustomerActiveStatus_ForUnknownCustomer_ReturnsBadRequest()
    {
        using var response = await api.AdminClient.PatchAsJsonAsync($"{CustomersUrl}/{Guid.NewGuid()}/status", new SetCustomerActiveStatusRequest(false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetCustomerActiveStatus_AsTechnician_ReturnsForbidden()
    {
        var id = await api.CreateCustomerAsync();

        using var response = await api.TechnicianClient.PatchAsJsonAsync($"{CustomersUrl}/{id}/status", new SetCustomerActiveStatusRequest(false));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
