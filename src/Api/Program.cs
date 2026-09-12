using CatCar.Contexts.ServiceOperations;
using CatCar.Contexts.CatalogInventory;
using CatCar.Contexts.Communication;
using CatCar.Contexts.IdentityAccess;
using CatCar.Contexts.IdentityAccess.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging ----
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture));

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

// ---- Ensure Database Schema (Development only) ----
// Uses EnsureCreatedAsync as the initial scaffold before formal EF Core migrations are introduced.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    foreach (var context in scope.ServiceProvider.GetServices<DbContext>())
    {
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    // Simulated bootstrap admin so the JWT-protected admin API is reachable without a
    // chicken-and-egg registration problem. Dev-only - see AdministrativeUserSeeder.
    await AdministrativeUserSeeder.SeedDefaultAdministratorAsync(app.Services).ConfigureAwait(false);
}

// ---- Middleware pipeline ----
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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
