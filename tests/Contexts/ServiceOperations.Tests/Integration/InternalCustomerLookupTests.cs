namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CatCar.Contexts.ServiceOperations.Features.Customers.RegisterCustomer;
using FluentAssertions;
using Xunit;

[Collection(ApiIntegrationGroup.Name)]
public sealed class InternalCustomerLookupTests(ApiFixture api)
{
    [Fact]
    public async Task LookupCustomer_WithValidKey_ReturnsOnlyIdentityAndStatus()
    {
        var document = ApiFixture.NewCpf();
        using var created = await api.AdminClient.PostAsJsonAsync(ApiFixture.ServiceOperationsPrefix + "/customers", new RegisterCustomerCommand(document, "Internal Customer", "85999990000", null));
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await created.Content.ReadFromJsonAsync<RegisterCustomerResult>();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/internal/v1/customers/by-document/{document}");
        request.Headers.Add("X-Internal-Api-Key", ApiFixture.InternalApiKey);
        using var response = await api.Client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("id", "isActive");
        json.RootElement.GetProperty("id").GetGuid().Should().Be(customer!.Id);
        json.RootElement.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }
}
