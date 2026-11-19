using CatCar.Contexts.ServiceOperations;
using CatCar.Contexts.CatalogInventory;
using CatCar.Contexts.Communication;
using CatCar.Contexts.IdentityAccess;
using CatCar.Contexts.IdentityAccess.Infrastructure.Seed;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contexts.CatalogInventory.Infrastructure;
using CatCar.Contexts.Communication.Infrastructure;
using CatCar.Contexts.IdentityAccess.Infrastructure;
using CatCar.Api;
using CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;
using CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;
using CatCar.Contexts.Communication.Infrastructure.Persistence;
using CatCar.Contexts.IdentityAccess.Infrastructure.Persistence;
using CatCar.Contexts.Communication.Notifications;
using CatCar.Contexts.CatalogInventory.Integrations;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging ----
RequestLogging.ConfigureHost(builder.Host);

// ---- Wolverine ----
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.ServiceOperations.Marker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.CatalogInventory.Marker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.Communication.Marker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.IdentityAccess.Marker).Assembly);
    // This class contains two handlers and does not follow the singular Handler suffix convention.
    opts.Discovery.IncludeType(typeof(CatalogSnapshotQueryHandlers));

    // EF's scoped contexts are factory-managed and must be shared with the outbox and repositories.
    opts.CodeGeneration.AlwaysUseServiceLocationFor<ServiceOperationsDbContext>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<CatalogInventoryDbContext>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<CommunicationDbContext>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IdentityAccessDbContext>();
    // The email transport is selected by an environment-dependent DI factory.
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IEmailSender>();

    opts.PersistMessagesWithPostgresql(builder.Configuration.GetConnectionString("catcar")!);
    opts.UseEntityFrameworkCoreTransactions();
    opts.OnException<DbUpdateConcurrencyException>()
        .RetryWithCooldown(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(1));
    opts.OnException<BudgetReservationOutcomeConflictException>()
        .RetryWithCooldown(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(1));
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
        scope.ServiceProvider.GetService<CatalogInventoryDbContext>(),
        scope.ServiceProvider.GetService<CommunicationDbContext>(),
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

if (args.Contains("--reconcile-inventory", StringComparer.Ordinal))
{
    await app.StartAsync().ConfigureAwait(false);
    try
    {
        await InventoryReservationReconciler.RunAsync(app.Services, CancellationToken.None).ConfigureAwait(false);
    }
    finally
    {
        await app.StopAsync().ConfigureAwait(false);
    }
    return;
}

app.Run();

public partial class Program;
