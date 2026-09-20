using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CatCar.Contexts.ServiceOperations.Tests;

public class GatewayKeyMiddlewareTests
{
    [Fact]
    public async Task Middleware_WhenGatewayKeyNotConfigured_AllowsRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/service-operations/work-orders";
        var nextCalled = false;

        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        // Act
        await ExecuteGatewayKeyCheckAsync(context, next, expectedGatewayKey: null);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Middleware_WhenHealthEndpoint_AllowsRequestWithoutHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/health/ready";
        var nextCalled = false;

        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        // Act
        await ExecuteGatewayKeyCheckAsync(context, next, expectedGatewayKey: "secret-key-32-chars-minimum-length");

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Middleware_WhenValidGatewayKeyProvided_AllowsRequest()
    {
        // Arrange
        const string gatewayKey = "secret-key-32-chars-minimum-length";
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/service-operations/work-orders";
        context.Request.Headers["X-CatCar-Gateway-Key"] = gatewayKey;
        var nextCalled = false;

        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        // Act
        await ExecuteGatewayKeyCheckAsync(context, next, expectedGatewayKey: gatewayKey);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Middleware_WhenMissingGatewayKey_ReturnsForbidden()
    {
        // Arrange
        const string gatewayKey = "secret-key-32-chars-minimum-length";
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/service-operations/work-orders";
        var nextCalled = false;

        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await ExecuteGatewayKeyCheckAsync(context, next, expectedGatewayKey: gatewayKey);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Middleware_WhenInvalidGatewayKey_ReturnsForbidden()
    {
        // Arrange
        const string gatewayKey = "secret-key-32-chars-minimum-length";
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/service-operations/work-orders";
        context.Request.Headers["X-CatCar-Gateway-Key"] = "wrong-gateway-key-value-123456789";
        var nextCalled = false;

        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await ExecuteGatewayKeyCheckAsync(context, next, expectedGatewayKey: gatewayKey);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
    }

    private static async Task ExecuteGatewayKeyCheckAsync(
        HttpContext context,
        RequestDelegate next,
        string? expectedGatewayKey)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.Equals("/health/live", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/health/ready", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/alive", StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (!string.IsNullOrEmpty(expectedGatewayKey))
        {
            if (!context.Request.Headers.TryGetValue("X-CatCar-Gateway-Key", out var headerValues) ||
                !headerValues.Any(v => string.Equals(v, expectedGatewayKey, StringComparison.Ordinal)))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Forbidden: Invalid or missing gateway key.").ConfigureAwait(false);
                return;
            }
        }

        await next(context).ConfigureAwait(false);
    }
}
