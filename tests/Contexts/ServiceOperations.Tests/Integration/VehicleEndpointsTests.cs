namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.GetVehicleById;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.ListVehiclesByCustomer;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.RegisterVehicle;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.SetVehicleActiveStatus;
using CatCar.Contexts.ServiceOperations.Features.Vehicles.UpdateVehicle;
using FluentAssertions;
using Xunit;

[Collection(ApiIntegrationGroup.Name)]
public sealed class VehicleEndpointsTests(ApiFixture api)
{
    private const string VehiclesUrl = ApiFixture.ServiceOperationsPrefix + "/vehicles";
    private const string CustomersUrl = ApiFixture.ServiceOperationsPrefix + "/customers";

    [Fact]
    public async Task RegisterVehicle_ForExistingCustomer_ReturnsCreatedWithNormalizedPlate()
    {
        var customerId = await api.CreateCustomerAsync();
        var plate = ApiFixture.NewPlate();

        using var response = await api.ReceptionistClient.PostAsJsonAsync(VehiclesUrl, new RegisterVehicleCommand(customerId, plate.ToLowerInvariant(), "Fiat", "Uno", 2015));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<RegisterVehicleResult>();
        created.Should().NotBeNull();
        created!.CustomerId.Should().Be(customerId);
        created.Plate.Should().Be(plate);
        created.IsActive.Should().BeTrue();
        response.Headers.Location!.ToString().Should().EndWith($"/vehicles/{created.Id}");
    }

    [Fact]
    public async Task RegisterVehicle_ForUnknownCustomer_ReturnsBadRequest()
    {
        using var response = await api.AdminClient.PostAsJsonAsync(VehiclesUrl, new RegisterVehicleCommand(Guid.NewGuid(), ApiFixture.NewPlate(), "Fiat", "Uno", 2015));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterVehicle_WithDuplicatedPlate_ReturnsBadRequest()
    {
        var customerId = await api.CreateCustomerAsync();
        var plate = ApiFixture.NewPlate();
        using var first = await api.AdminClient.PostAsJsonAsync(VehiclesUrl, new RegisterVehicleCommand(customerId, plate, "Fiat", "Uno", 2015));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        using var second = await api.AdminClient.PostAsJsonAsync(VehiclesUrl, new RegisterVehicleCommand(customerId, plate, "VW", "Gol", 2018));

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("AB12")]
    [InlineData("")]
    public async Task RegisterVehicle_WithInvalidPlate_ReturnsBadRequest(string plate)
    {
        var customerId = await api.CreateCustomerAsync();

        using var response = await api.AdminClient.PostAsJsonAsync(VehiclesUrl, new RegisterVehicleCommand(customerId, plate, "Fiat", "Uno", 2015));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterVehicle_WithoutBrand_ReturnsBadRequest()
    {
        var customerId = await api.CreateCustomerAsync();

        using var response = await api.AdminClient.PostAsJsonAsync(VehiclesUrl, new RegisterVehicleCommand(customerId, ApiFixture.NewPlate(), string.Empty, "Uno", 2015));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterVehicle_AsTechnician_ReturnsForbidden()
    {
        var customerId = await api.CreateCustomerAsync();

        using var response = await api.TechnicianClient.PostAsJsonAsync(VehiclesUrl, new RegisterVehicleCommand(customerId, ApiFixture.NewPlate(), "Fiat", "Uno", 2015));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RegisterVehicle_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.PostAsJsonAsync(VehiclesUrl, new RegisterVehicleCommand(Guid.NewGuid(), ApiFixture.NewPlate(), "Fiat", "Uno", 2015));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetVehicleById_ForExistingVehicle_ReturnsDetails()
    {
        var customerId = await api.CreateCustomerAsync();
        var vehicleId = await api.CreateVehicleAsync(customerId);

        using var response = await api.TechnicianClient.GetAsync($"{VehiclesUrl}/{vehicleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<VehicleDetails>();
        details!.Id.Should().Be(vehicleId);
        details.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public async Task GetVehicleById_ForUnknownVehicle_ReturnsNotFound()
    {
        using var response = await api.AdminClient.GetAsync($"{VehiclesUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetVehicleById_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.GetAsync($"{VehiclesUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListVehiclesByCustomer_ReturnsOnlyThatCustomersVehiclesAndHonorsActiveFilter()
    {
        var customerId = await api.CreateCustomerAsync();
        var otherCustomerId = await api.CreateCustomerAsync();
        var activeVehicleId = await api.CreateVehicleAsync(customerId);
        var inactiveVehicleId = await api.CreateVehicleAsync(customerId);
        var otherVehicleId = await api.CreateVehicleAsync(otherCustomerId);
        using var deactivate = await api.AdminClient.PatchAsJsonAsync($"{VehiclesUrl}/{inactiveVehicleId}/status", new SetVehicleActiveStatusRequest(false));
        deactivate.StatusCode.Should().Be(HttpStatusCode.OK);

        var all = await api.AdminClient.GetFromJsonAsync<List<VehicleSummary>>($"{CustomersUrl}/{customerId}/vehicles");
        var onlyActive = await api.AdminClient.GetFromJsonAsync<List<VehicleSummary>>($"{CustomersUrl}/{customerId}/vehicles?onlyActive=true");

        all.Should().Contain(v => v.Id == activeVehicleId).And.Contain(v => v.Id == inactiveVehicleId).And.NotContain(v => v.Id == otherVehicleId);
        onlyActive.Should().Contain(v => v.Id == activeVehicleId).And.NotContain(v => v.Id == inactiveVehicleId);
    }

    [Fact]
    public async Task ListVehiclesByCustomer_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.GetAsync($"{CustomersUrl}/{Guid.NewGuid()}/vehicles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateVehicle_WithValidData_PersistsChanges()
    {
        var customerId = await api.CreateCustomerAsync();
        var vehicleId = await api.CreateVehicleAsync(customerId);

        using var response = await api.ReceptionistClient.PutAsJsonAsync($"{VehiclesUrl}/{vehicleId}", new UpdateVehicleRequest("Volkswagen", "Gol", 2020));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<UpdateVehicleResult>();
        updated!.Brand.Should().Be("Volkswagen");
        updated.Model.Should().Be("Gol");
        updated.ManufactureYear.Should().Be(2020);

        var persisted = await api.AdminClient.GetFromJsonAsync<VehicleDetails>($"{VehiclesUrl}/{vehicleId}");
        persisted!.Brand.Should().Be("Volkswagen");
    }

    [Fact]
    public async Task UpdateVehicle_ForUnknownVehicle_ReturnsBadRequest()
    {
        using var response = await api.AdminClient.PutAsJsonAsync($"{VehiclesUrl}/{Guid.NewGuid()}", new UpdateVehicleRequest("Volkswagen", "Gol", 2020));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateVehicle_WithoutModel_ReturnsBadRequest()
    {
        var customerId = await api.CreateCustomerAsync();
        var vehicleId = await api.CreateVehicleAsync(customerId);

        using var response = await api.AdminClient.PutAsJsonAsync($"{VehiclesUrl}/{vehicleId}", new UpdateVehicleRequest("Volkswagen", string.Empty, 2020));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateVehicle_AsTechnician_ReturnsForbidden()
    {
        using var response = await api.TechnicianClient.PutAsJsonAsync($"{VehiclesUrl}/{Guid.NewGuid()}", new UpdateVehicleRequest("Volkswagen", "Gol", 2020));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetVehicleActiveStatus_TogglesVehicleStatus()
    {
        var customerId = await api.CreateCustomerAsync();
        var vehicleId = await api.CreateVehicleAsync(customerId);

        using var deactivated = await api.AdminClient.PatchAsJsonAsync($"{VehiclesUrl}/{vehicleId}/status", new SetVehicleActiveStatusRequest(false));
        var afterDeactivation = await api.AdminClient.GetFromJsonAsync<VehicleDetails>($"{VehiclesUrl}/{vehicleId}");
        using var reactivated = await api.AdminClient.PatchAsJsonAsync($"{VehiclesUrl}/{vehicleId}/status", new SetVehicleActiveStatusRequest(true));
        var afterReactivation = await api.AdminClient.GetFromJsonAsync<VehicleDetails>($"{VehiclesUrl}/{vehicleId}");

        deactivated.StatusCode.Should().Be(HttpStatusCode.OK);
        reactivated.StatusCode.Should().Be(HttpStatusCode.OK);
        afterDeactivation!.IsActive.Should().BeFalse();
        afterReactivation!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SetVehicleActiveStatus_ForUnknownVehicle_ReturnsBadRequest()
    {
        using var response = await api.AdminClient.PatchAsJsonAsync($"{VehiclesUrl}/{Guid.NewGuid()}/status", new SetVehicleActiveStatusRequest(false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetVehicleActiveStatus_AsTechnician_ReturnsForbidden()
    {
        using var response = await api.TechnicianClient.PatchAsJsonAsync($"{VehiclesUrl}/{Guid.NewGuid()}/status", new SetVehicleActiveStatusRequest(false));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
