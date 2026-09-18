using CatCar.Contexts.ServiceOperations;
using CatCar.Contexts.CatalogInventory;
using CatCar.Contexts.Communication;
using CatCar.Contexts.IdentityAccess;
using CatCar.Contexts.IdentityAccess.Infrastructure.Seed;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contexts.CatalogInventory.Infrastructure;
using CatCar.Contexts.Communication.Infrastructure;
using CatCar.Contexts.IdentityAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging ----
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter()));

// ---- Wolverine ----
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.ServiceOperations.Marker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.CatalogInventory.Marker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.Communication.Marker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.IdentityAccess.Marker).Assembly);

    opts.PersistMessagesWithPostgresql(builder.Configuration.GetConnectionString("catcar")!);
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.UseDurableLocalQueues();
});

// ---- Aspire ServiceDefaults ----
builder.AddServiceDefaults();

// ---- BC Modules ----
builder.Services.AddServiceOperations(builder.Configuration);
builder.Services.AddCatalogInventory(builder.Configuration);
builder.Services.AddCommunication(builder.Configuration);
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
if (app.Environment.IsDevelopment() || migrationOnly)
{
    using var scope = app.Services.CreateScope();
    var dbContexts = new DbContext?[]
    {
        scope.ServiceProvider.GetService<ServiceOperationsDbContext>(),
        scope.ServiceProvider.GetService<CatalogInventoryDbContext>(),
        scope.ServiceProvider.GetService<CommunicationDbContext>(),
        scope.ServiceProvider.GetService<IdentityAccessDbContext>()
    };
    foreach (var context in dbContexts.OfType<DbContext>())
    {
        await context.Database.MigrateAsync().ConfigureAwait(false);
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
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.CreateVersion7().ToString();
    }

    context.TraceIdentifier = correlationId;
    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        return Task.CompletedTask;
    });

    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next().ConfigureAwait(false);
    }
});

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("docs", options =>
    {
        options.WithTitle("CatCar API Documentation")
               .WithTheme(ScalarTheme.Purple)
               .WithDefaultHttpClient(ScalarTarget.Http, ScalarClient.Http11);
    });

    app.MapGet("/", () => Results.Redirect("/docs"))
       .ExcludeFromDescription();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// ---- MapGroup per BC ----
app.MapGroup("/api/v1/service-operations")
    .MapServiceOperationsEndpoints();

app.MapGroup("/api/v1/catalog-inventory")
    .MapCatalogInventoryEndpoints();

app.MapGroup("/api/v1/communication")
    .MapCommunicationEndpoints();

app.MapGroup("/api/v1/identity-access")
    .MapIdentityAccessEndpoints();

// ---- Health ----
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });

// ---- Aspire default endpoints (/health overall, /alive liveness) ----
app.MapDefaultEndpoints();

app.Run();
