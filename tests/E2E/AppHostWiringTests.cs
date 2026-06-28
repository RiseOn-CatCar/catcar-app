using System.Net;
using Aspire.Hosting.ApplicationModel;
using FluentAssertions;

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
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("api", KnownResourceStates.Running, CancellationToken.None);
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("postgres", KnownResourceStates.Running, CancellationToken.None);
    }

    [Fact]
    public async Task ApiHealthEndpoint_ReturnsOk()
    {
        // covers: AC-042
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("api", KnownResourceStates.Running, CancellationToken.None);

        using var client = _fixture.Application.CreateHttpClient("api");
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostgresResource_IsRunning()
    {
        // covers: AC-042, AC-044
        await _fixture.Application.ResourceNotifications.WaitForResourceAsync("postgres", KnownResourceStates.Running, CancellationToken.None);
    }
}
