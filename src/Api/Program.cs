using CatCar.Contexts.ServiceOperations;
using CatCar.Contexts.ServiceOperations.Features.Customers.LookupCustomerForAuthentication;
using CatCar.Contexts.IdentityAccess;
using CatCar.Contexts.IdentityAccess.Infrastructure.Seed;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contexts.ServiceOperations.Integrations;
using CatCar.Contexts.IdentityAccess.Infrastructure;
using CatCar.Api;
using CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;
using CatCar.Contexts.IdentityAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging ----
RequestLogging.ConfigureHost(builder.Host);

// ---- Wolverine ----
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.ServiceOperations.Marker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.IdentityAccess.Marker).Assembly);

    // EF's scoped contexts are factory-managed and must be shared with the outbox and repositories.
    opts.CodeGeneration.AlwaysUseServiceLocationFor<ServiceOperationsDbContext>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IdentityAccessDbContext>();
    // The catalog ACL is a typed HttpClient registered through IHttpClientFactory.
    opts.CodeGeneration.AlwaysUseServiceLocationFor<ICatalogInventoryAcl>();

    opts.PersistMessagesWithPostgresql(builder.Configuration.GetConnectionString("catcar")!);
    opts.UseEntityFrameworkCoreTransactions();
    opts.OnException<DbUpdateConcurrencyException>()
        .RetryWithCooldown(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(1));
    opts.Policies.UseDurableLocalQueues();
    opts.UseRabbitMq(new Uri(builder.Configuration.GetConnectionString("messaging")!))
        .AutoProvision()
        .UseConventionalRouting();
    opts.Policies.UseDurableInboxOnAllListeners();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
});

// ---- Aspire ServiceDefaults ----
builder.AddServiceDefaults();

// ---- BC Modules ----
builder.Services.AddServiceOperations(builder.Configuration);
builder.Services.AddIdentityAccess(builder.Configuration);

// ---- OpenAPI ----
builder.Services.AddOpenApi();

// ---- ProblemDetails (RFC 7807) ----
builder.Services.AddProblemDetails();

// ---- Health Checks ----
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("catcar")!, name: "postgres", tags: ["ready"]);

var app = builder.Build();

// ---- Apply Database Migrations ----
// Development applies migrations at startup. The production migration Job passes
// --migrate, applies every context migration, and exits before starting the HTTP host.
var migrationOnly = args.Contains("--migrate", StringComparer.Ordinal);
var provisionReadonly = args.Contains("--provision-auth-readonly", StringComparer.Ordinal);
var bootstrapAdmin = args.Contains("--bootstrap-admin", StringComparer.Ordinal);
if ((provisionReadonly || bootstrapAdmin) && !migrationOnly)
    throw new InvalidOperationException("Provisioning flags require --migrate.");
if (app.Environment.IsDevelopment() || migrationOnly)
{
    using var scope = app.Services.CreateScope();
    var dbContexts = new DbContext?[]
    {
        scope.ServiceProvider.GetService<ServiceOperationsDbContext>(),
        scope.ServiceProvider.GetService<IdentityAccessDbContext>()
    };
    foreach (var context in dbContexts.OfType<DbContext>())
    {
        await context.Database.MigrateAsync().ConfigureAwait(false);
    }

    if (provisionReadonly)
    {
        await AuthReadOnlyRoleProvisioner.ProvisionAsync(
            builder.Configuration.GetConnectionString("catcar")!,
            builder.Configuration["AuthReadOnly:Login"]!,
            builder.Configuration["AuthReadOnly:Password"]!,
            CancellationToken.None).ConfigureAwait(false);
    }
    if (bootstrapAdmin)
    {
        await AdministrativeUserSeeder.EnsureAdministratorAsync(app.Services,
            builder.Configuration["BootstrapAdmin:Email"]!,
            builder.Configuration["BootstrapAdmin:Password"]!).ConfigureAwait(false);
    }

    if (app.Environment.IsDevelopment())
    {
        // Simulated bootstrap admin so the JWT-protected admin API is reachable without a
        // chicken-and-egg registration problem. Dev-only - see AdministrativeUserSeeder.
        await AdministrativeUserSeeder.SeedDefaultAdministratorAsync(app.Services).ConfigureAwait(false);
    }
}

if (migrationOnly)
{
    return;
}

// ---- Middleware pipeline ----
app.Use(async (context, next) =>
{
    var correlationIdHeader = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
    var correlationId = Guid.TryParse(correlationIdHeader, out var parsedCorrelationId) && parsedCorrelationId != Guid.Empty
        ? parsedCorrelationId
        : Guid.CreateVersion7();
    var correlationIdValue = correlationId.ToString();

    context.TraceIdentifier = correlationIdValue;
    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationIdValue;
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Correlation-Id"] = correlationIdValue;
        return Task.CompletedTask;
    });

    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationIdValue))
    {
        await next().ConfigureAwait(false);
    }
});

app.UseSafeRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("LocalKubernetes"))
{
    // Keep /docs directly accessible while Scalar's relative assets resolve from
    // the root URL used by browsers for a path without a trailing slash.
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.Equals("/docs", StringComparison.OrdinalIgnoreCase))
            context.Request.Path = "/docs/v1";
        else if (context.Request.Path.Equals("/scalar.js", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.Equals("/scalar.aspnetcore.js", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.Equals("/favicon.svg", StringComparison.OrdinalIgnoreCase))
            context.Request.Path = "/docs" + context.Request.Path;

        await next(context).ConfigureAwait(false);
    });

    app.MapOpenApi();
    app.MapScalarApiReference("docs/", options =>
    {
        options.WithTitle("CatCar API Documentation")
               .WithTheme(ScalarTheme.Purple)
               .WithDefaultHttpClient(ScalarTarget.Http, ScalarClient.Http11);
    });

    app.MapGet("/", () => Results.Redirect("/docs"))
       .ExcludeFromDescription();
}

app.UseRouting();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// ---- MapGroup per BC ----
app.MapGroup("/api/v1/service-operations")
    .MapServiceOperationsEndpoints();

app.MapLookupCustomerForAuthenticationEndpoint();

app.MapGroup("/api/v1/identity-access")
    .MapIdentityAccessEndpoints();

// ---- Health ----
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });

// ---- Aspire default endpoints (/health overall, /alive liveness) ----
app.MapDefaultEndpoints();


app.Run();

public partial class Program;
