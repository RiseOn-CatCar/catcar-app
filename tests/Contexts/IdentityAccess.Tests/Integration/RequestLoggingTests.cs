namespace CatCar.Contexts.IdentityAccess.Tests.Integration;

using System.Collections.Concurrent;
using System.Diagnostics;
using CatCar.Api;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Xunit;

public sealed class RequestLoggingTests
{
    [Fact]
    public async Task RequestLogging_ApprovalLink_ExcludesTokenAndForwardsOnce()
    {
        const string token = "sensitive-approval-token-72";
        const string queryToken = "sensitive-query-token-49";
        var capture = new CaptureProvider();
        var activities = new CaptureExporter();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddProvider(capture);
        RequestLogging.ConfigureHost(builder.Host);
        builder.AddServiceDefaults();
        builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddProcessor(new SimpleActivityExportProcessor(activities)));
        await using var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.UseSafeRequestLogging();
        app.MapGet("/api/v1/communication/approval-links/{rawToken}", () => "ok");
        app.MapGet("/probe/{id}", () => "ok");
        await app.StartAsync();
        try
        {
            var log = LoggerMessage.Define(LogLevel.Information, new EventId(9812, "BridgeProbe"), "Bridge probe");
            log(app.Logger, null);
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient();
            // The in-process client is not part of the server under test.
            using var suppression = SuppressInstrumentationScope.Begin();
            using var response = await client.GetAsync(new Uri(new Uri(address), $"/api/v1/communication/approval-links/{token}?access_token={queryToken}"));
            response.EnsureSuccessStatusCode();
            using var tracedResponse = await client.GetAsync(new Uri(new Uri(address), $"/probe/{token}?access_token={queryToken}"));
            tracedResponse.EnsureSuccessStatusCode();
            capture.Entries.Count(entry => entry.Contains("Bridge probe", StringComparison.Ordinal)).Should().Be(1);
            capture.Entries.Should().Contain(entry => entry.Contains("HTTP request completed", StringComparison.Ordinal));
            capture.Entries.Count(entry => entry.Contains("HTTP request completed", StringComparison.Ordinal)).Should().Be(2);
            capture.Entries.Should().OnlyContain(entry => !entry.Contains(token, StringComparison.Ordinal));
            capture.Entries.Should().OnlyContain(entry => !entry.Contains(queryToken, StringComparison.Ordinal));
            activities.Entries.Should().OnlyContain(entry => !entry.Contains(token, StringComparison.Ordinal));
            activities.Entries.Should().NotBeEmpty();
            activities.Entries.Should().OnlyContain(entry => !entry.Contains(queryToken, StringComparison.Ordinal));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private sealed class CaptureProvider : ILoggerProvider
    {
        public ConcurrentBag<string> Entries { get; } = [];
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(Entries);
        public void Dispose() { }
    }

    private sealed class CaptureLogger(ConcurrentBag<string> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            entries.Add(formatter(state, exception) + " " + string.Join(" ", state is IEnumerable<KeyValuePair<string, object?>> properties
                ? properties.Select(property => $"{property.Key}={property.Value}") : []));
        }
    }

    private sealed class CaptureExporter : BaseExporter<Activity>
    {
        public ConcurrentBag<string> Entries { get; } = [];
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch)
                Entries.Add(activity.DisplayName + " " + string.Join(" ", activity.TagObjects.Select(tag => $"{tag.Key}={tag.Value}")));
            return ExportResult.Success;
        }
    }
}
