using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Aspire.Hosting.ApplicationModel;
using FluentAssertions;
using Npgsql;

namespace CatCar.E2E.Tests;

/// <summary>
/// E2E wiring tests for the Aspire AppHost.
/// Uses the shared <see cref="AppHostFixture"/> via the <c>AppHost</c> collection.
/// covers: AC-043 (IAsyncLifetime fixture lifecycle)
/// </summary>
[Collection("AppHost")]
public class AppHostWiringTests
{
    private readonly AppHostFixture _fixture;

    public AppHostWiringTests(AppHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AppHostStarts_AndAllResourcesBecomeHealthy()
    {
        // covers: AC-042, AC-044
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("postgres", KnownResourceStates.Running, CancellationToken.None);
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("api", KnownResourceStates.Running, CancellationToken.None);
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("auth-storage", KnownResourceStates.Running, CancellationToken.None);
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("auth-function", KnownResourceStates.Running, CancellationToken.None);
    }

    [Fact]
    public async Task ApiHealthEndpoint_ReturnsOk()
    {
        // covers: AC-042
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("api", KnownResourceStates.Running, CancellationToken.None);

        using var client = _fixture.Application.CreateHttpClient("api");
        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiOpenApiDocument_OpenWorkOrderRequestDoesNotExposeCorrelationId()
    {
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("api", KnownResourceStates.Running, CancellationToken.None);

        using var client = _fixture.Application.CreateHttpClient("api");
        using var response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var documentStream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(documentStream);
        var postOperation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/service-operations/work-orders")
            .GetProperty("post");
        var requestSchema = postOperation
            .GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema");

        if (requestSchema.TryGetProperty("$ref", out var schemaReference))
        {
            var schemaName = schemaReference.GetString()!.Split('/').Last();
            requestSchema = document.RootElement
                .GetProperty("components")
                .GetProperty("schemas")
                .GetProperty(schemaName);
        }

        requestSchema.GetProperty("properties").TryGetProperty("correlationId", out _).Should().BeFalse();
    }

    [Fact]
    public async Task AuthFunctionOpenApiDocument_DescribesOnlyCustomerPostOperation()
    {
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("auth-function", KnownResourceStates.Running, CancellationToken.None);

        using var client = _fixture.Application.CreateHttpClient("auth-function");
        using var response = await client.GetAsync("/api/openapi/v3.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var documentStream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(documentStream);
        document.RootElement.GetProperty("openapi").GetString().Should().StartWith("3.");

        var pathItem = document.RootElement
            .GetProperty("paths")
            .GetProperty("/auth/customer");
        pathItem.EnumerateObject().Select(property => property.Name).Should().Equal("post");

        var operation = pathItem.GetProperty("post");
        var requestBody = operation.GetProperty("requestBody");
        requestBody.GetProperty("required").GetBoolean().Should().BeTrue();
        var requestSchema = requestBody
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema");
        if (requestSchema.TryGetProperty("$ref", out var schemaReference))
        {
            var schemaName = schemaReference.GetString()!.Split('/').Last();
            requestSchema = document.RootElement
                .GetProperty("components")
                .GetProperty("schemas")
                .GetProperty(schemaName);
        }

        requestSchema.GetProperty("required").EnumerateArray()
            .Select(item => item.GetString())
            .Should().Contain("documentNumber");
        operation.GetProperty("responses").EnumerateObject()
            .Select(responseProperty => responseProperty.Name)
            .Should().Contain(["200", "400", "403", "404"]);
    }

    [Fact]
    public async Task ApiHealthEndpoint_WithCorrelationId_ShouldEchoCorrelationId()
    {
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("api", KnownResourceStates.Running, CancellationToken.None);

        using var client = _fixture.Application.CreateHttpClient("api");
        var correlationId = Guid.CreateVersion7().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);
        using var response = await client.GetAsync("/health/live");

        response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle().Which.Should().Be(correlationId);
    }

    [Fact]
    public async Task ApiHealthEndpoint_WithoutCorrelationId_ShouldGenerateGuidCorrelationId()
    {
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("api", KnownResourceStates.Running, CancellationToken.None);

        using var client = _fixture.Application.CreateHttpClient("api");
        using var response = await client.GetAsync("/health/live");

        var correlationId = response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle().Which;
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CustomerAuthenticationEndpoint_ReturnsBearerToken()
    {
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("api", KnownResourceStates.Running, CancellationToken.None);
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("auth-storage", KnownResourceStates.Running, CancellationToken.None);

        using (var apiClient = _fixture.Application.CreateHttpClient("api"))
        {
            var healthResponse = await apiClient.GetAsync("/health/ready");
            healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var connectionString = await _fixture.Application.GetConnectionStringAsync("catcar");
        await using var dataSource = NpgsqlDataSource.Create(connectionString!);
        await using var cmd = dataSource.CreateCommand("""
            INSERT INTO service_operations.customers (id, document_number, document_type, name, phone, email, is_active, created_at, updated_at, created_by, updated_by)
            VALUES (@id, @doc, 'CPF', 'Customer Test', '11999999999', 'test@example.com', true, NOW(), NOW(), 'seed', 'seed')
            ON CONFLICT (document_number) DO NOTHING;
            """);
        cmd.Parameters.AddWithValue("id", Guid.CreateVersion7());
        cmd.Parameters.AddWithValue("doc", "52998224725");
        await cmd.ExecuteNonQueryAsync();

        using var client = _fixture.Application.CreateHttpClient("auth-function");
        var response = await client.PostAsJsonAsync(
            "/api/auth/customer",
            new { documentNumber = "52998224725" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authenticationResponse = await response.Content.ReadFromJsonAsync<CustomerAuthenticationResponse>();

        authenticationResponse.Should().NotBeNull();
        authenticationResponse!.Token.Should().NotBeNullOrWhiteSpace();
        authenticationResponse.ExpiresIn.Should().Be(3600);
        authenticationResponse.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task PostgresResource_IsRunning()
    {
        // covers: AC-042, AC-044
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("postgres", KnownResourceStates.Running, CancellationToken.None);
    }

    private sealed record CustomerAuthenticationResponse(string Token, int ExpiresIn, string TokenType);
}
