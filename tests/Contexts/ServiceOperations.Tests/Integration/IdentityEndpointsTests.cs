namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CatCar.Contexts.IdentityAccess.Features.AdministrativeUsers.RegisterAdministrativeUser;
using CatCar.Contexts.IdentityAccess.Features.Authentication.Login;
using FluentAssertions;
using Xunit;

[Collection(ApiIntegrationGroup.Name)]
public sealed class IdentityEndpointsTests(ApiFixture api)
{
    private const string LoginUrl = ApiFixture.IdentityAccessPrefix + "/auth/login";
    private const string UsersUrl = ApiFixture.IdentityAccessPrefix + "/administrative-users";

    [Fact]
    public async Task Login_WithSeededAdministrator_ReturnsUsableToken()
    {
        var (status, token) = await api.LoginAsync(ApiFixture.AdminEmail, ApiFixture.AdminPassword);

        status.Should().Be(HttpStatusCode.OK);
        token.Should().NotBeNullOrWhiteSpace();

        using var client = api.CreateClient("Administrador");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.GetAsync($"{ApiFixture.ServiceOperationsPrefix}/customers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ReturnsRoleAndExpiration()
    {
        using var response = await api.Client.PostAsJsonAsync(LoginUrl, new LoginCommand(ApiFixture.AdminEmail, ApiFixture.AdminPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResult>();
        body.Should().NotBeNull();
        body!.Role.Should().Be("Administrador");
        body.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Theory]
    [InlineData("admin@catcar.dev.br", "wrong-password")]
    [InlineData("nobody@catcar.dev.br", "Catcar@Admin123")]
    [InlineData("", "")]
    [InlineData("not-an-email", "Catcar@Admin123")]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized(string email, string password)
    {
        var (status, token) = await api.LoginAsync(email, password);

        status.Should().Be(HttpStatusCode.Unauthorized);
        token.Should().BeNull();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.GetAsync($"{ApiFixture.ServiceOperationsPrefix}/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        var expired = ApiFixture.CreateToken("Administrador", expiresAtUtc: DateTime.UtcNow.AddMinutes(-30));
        using var client = api.CreateClient("Administrador");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expired);

        using var response = await client.GetAsync($"{ApiFixture.ServiceOperationsPrefix}/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMalformedToken_ReturnsUnauthorized()
    {
        using var client = api.CreateClient("Administrador");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.jwt");

        using var response = await client.GetAsync($"{ApiFixture.ServiceOperationsPrefix}/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterAdministrativeUser_AsAdministrator_CreatesUserThatCanLogIn()
    {
        var email = $"user-{Guid.NewGuid():N}@catcar.test";
        const string password = "Str0ng-Password!";

        using var response = await api.AdminClient.PostAsJsonAsync(UsersUrl, new RegisterAdministrativeUserCommand("Front Desk", email, password, "Recepcionista"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<RegisterAdministrativeUserResult>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();

        var (status, token) = await api.LoginAsync(email, password);
        status.Should().Be(HttpStatusCode.OK);
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RegisterAdministrativeUser_WithInvalidRole_ReturnsBadRequest()
    {
        using var response = await api.AdminClient.PostAsJsonAsync(
            UsersUrl, new RegisterAdministrativeUserCommand("Someone", $"user-{Guid.NewGuid():N}@catcar.test", "Str0ng-Password!", "Pirate"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterAdministrativeUser_WithShortPassword_ReturnsBadRequest()
    {
        using var response = await api.AdminClient.PostAsJsonAsync(
            UsersUrl, new RegisterAdministrativeUserCommand("Someone", $"user-{Guid.NewGuid():N}@catcar.test", "short", "Tecnico"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("Recepcionista")]
    [InlineData("Tecnico")]
    public async Task RegisterAdministrativeUser_WithoutAdministratorRole_ReturnsForbidden(string role)
    {
        using var client = api.CreateClient(role);

        using var response = await client.PostAsJsonAsync(
            UsersUrl, new RegisterAdministrativeUserCommand("Someone", $"user-{Guid.NewGuid():N}@catcar.test", "Str0ng-Password!", "Tecnico"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RegisterAdministrativeUser_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await api.Client.PostAsJsonAsync(
            UsersUrl, new RegisterAdministrativeUserCommand("Someone", $"user-{Guid.NewGuid():N}@catcar.test", "Str0ng-Password!", "Tecnico"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
