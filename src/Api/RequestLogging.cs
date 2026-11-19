namespace CatCar.Api;

using System.Diagnostics;
using Microsoft.AspNetCore.Routing;
using Serilog;

public static class RequestLogging
{
    public static void ConfigureHost(IHostBuilder host) => host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With<RemoveRequestUrlEnricher>()
            .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter()), writeToProviders: true);

    public static void UseSafeRequestLogging(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context).ConfigureAwait(false);
            }
            finally
            {
                // Server instrumentation starts before middleware and can include raw URLs.
                if (Activity.Current is { } activity)
                {
                    activity.SetTag("url.full", null);
                    activity.SetTag("url.path", null);
                    activity.SetTag("url.query", null);
                    activity.SetTag("http.url", null);
                    activity.SetTag("http.target", null);
                }
            }
        });

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP request completed";
            options.EnrichDiagnosticContext = (diagnostic, context) =>
            {
                diagnostic.Set("RequestMethod", context.Request.Method);
                diagnostic.Set("StatusCode", context.Response.StatusCode);
                diagnostic.Set("RouteTemplate", (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "<unmatched>");
            };
        });
    }

    private sealed class RemoveRequestUrlEnricher : Serilog.Core.ILogEventEnricher
    {
        public void Enrich(Serilog.Events.LogEvent logEvent, Serilog.Core.ILogEventPropertyFactory propertyFactory)
        {
            logEvent.RemovePropertyIfPresent("RequestPath");
            logEvent.RemovePropertyIfPresent("QueryString");
        }
    }
}
