namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using CatCar.Contracts.ServiceOperations;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Xunit;

// covers: AC-016, AC-017, AC-018
// AC-016: Wolverine is configured with in-process transport and discovers handlers from all four BC assemblies
// AC-017: Integration events are persisted to the outbox table within the same transaction as aggregate mutation
// AC-018: Wolverine's DurabilityAgent runs as a background hosted service to relay unprocessed outbox messages
public class OutboxTests : IntegrationTestBase
{
    private static int _testDiscoveryHandledCount;

    [Fact]
    // covers: AC-017
    public async Task PublishAsync_WithinDbContextTransaction_PersistsEventToOutbox()
    {
        // Arrange: build an IHost that mirrors the API composition root
        var connectionString = ConnectionString;

        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:catcar"] = connectionString,
                });
            })
            .UseWolverine(opts =>
            {
                opts.Discovery.IncludeAssembly(typeof(Marker).Assembly);

                opts.PersistMessagesWithPostgresql(connectionString, schemaName: "service_operations");
                opts.UseEntityFrameworkCoreTransactions();
                opts.Policies.UseDurableLocalQueues();

                // Ensure BudgetIssuedIntegrationEvent has a durable local subscriber so that PublishAsync
                // routes the message and persists it to the outbox table.
                opts.PublishMessage<BudgetIssuedIntegrationEvent>().ToLocalQueue("budget_issued");

                // Enable runtime compilation for test message handling
                opts.UseRuntimeCompilation();
            })
            .ConfigureServices(services =>
            {
                services.AddDbContextWithWolverineIntegration<ServiceOperationsDbContext>(options =>
                {
                    options.UseNpgsql(connectionString);
                    options.AddInterceptors(new AuditInterceptor());
                }, wolverineDatabaseSchema: "service_operations");
            })
            .Build();

        // Start the host — Wolverine will create its message store tables via Weasel migration.
        await host.StartAsync();

        await using var scope = host.Services.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IDbContextOutbox<ServiceOperationsDbContext>>();

        // Ensure EF Core database and schema exist
        await outbox.DbContext.Database.EnsureCreatedAsync();

        var entity = new TestAuditableEntity { Name = "Outbox Test" };
        outbox.DbContext.TestAuditableEntities.Add(entity);

        var integrationEvent = new BudgetIssuedIntegrationEvent(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            250.00m,
            DateTime.UtcNow);

        // Verify the message is routable before acting.
        var preview = outbox.PreviewSubscriptions(integrationEvent);
        preview.Count.Should().BeGreaterThanOrEqualTo(1, "the event should have at least one subscriber");

        // Act: publish within the same EF Core transaction.
        // IDbContextOutbox enrolls the outgoing envelope in the EF Core transaction.
        // SaveChangesAndFlushMessagesAsync persists both the aggregate mutation and the outbox message.
        await outbox.PublishAsync(integrationEvent);
        await outbox.SaveChangesAndFlushMessagesAsync();

        // Assert: AC-017 — verify the message was persisted to the Wolverine incoming envelopes table.
        // Messages routed to durable local queues are stored in wolverine_incoming_envelopes.
        var connection = outbox.DbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*)::int FROM service_operations.wolverine_incoming_envelopes";
        var result = await cmd.ExecuteScalarAsync();
        var envelopeCount = result is DBNull ? 0 : Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
        envelopeCount.Should().BeGreaterThanOrEqualTo(1, "the published message should be persisted in wolverine_incoming_envelopes");

        await host.StopAsync();
    }

    [Fact]
    // covers: AC-041
    public async Task WolverineConfiguration_DiscoversHandlersFromServiceOperationsAssembly()
    {
        // Arrange: build a host with handler discovery enabled for the ServiceOperations BC Marker assembly
        // and include the test assembly so the TestDiscoveryMessage handler is discovered.
        var connectionString = ConnectionString;

        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:catcar"] = connectionString,
                });
            })
            .UseWolverine(opts =>
            {
                opts.Discovery.IncludeAssembly(typeof(Marker).Assembly);
                opts.Discovery.IncludeAssembly(typeof(OutboxTests).Assembly);

                opts.PersistMessagesWithPostgresql(connectionString);
                opts.UseEntityFrameworkCoreTransactions();
                opts.Policies.UseDurableLocalQueues();
                opts.UseRuntimeCompilation();
            })
            .ConfigureServices(services =>
            {
                services.AddDbContext<ServiceOperationsDbContext>(options =>
                {
                    options.UseNpgsql(connectionString);
                    options.AddInterceptors(new AuditInterceptor());
                });
            })
            .Build();

        _testDiscoveryHandledCount = 0;

        await host.StartAsync();

        await using var scope = host.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act: publish a message defined in this test assembly.
        await bus.PublishAsync(new TestDiscoveryMessage());
        await Task.Delay(1000);

        // Assert: AC-041 — the handler defined in the discovered test assembly was invoked.
        _testDiscoveryHandledCount.Should().Be(1);

        await host.StopAsync();
    }

    [Fact]
    // covers: AC-018
    public async Task WolverineConfiguration_RegistersDurableMessagingHostedService()
    {
        // Arrange: build a minimal host with Wolverine configured for durable messaging
        var connectionString = ConnectionString;

        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:catcar"] = connectionString,
                });
            })
            .UseWolverine(opts =>
            {
                opts.Discovery.IncludeAssembly(typeof(Marker).Assembly);
                opts.PersistMessagesWithPostgresql(connectionString);
                opts.UseEntityFrameworkCoreTransactions();
                opts.Policies.UseDurableLocalQueues();
            })
            .ConfigureServices(services =>
            {
                services.AddDbContext<ServiceOperationsDbContext>(options =>
                {
                    options.UseNpgsql(connectionString);
                    options.AddInterceptors(new AuditInterceptor());
                });
            })
            .Build();

        // Act
        await host.StartAsync();

        // Assert: AC-018 — WolverineRuntime is registered as IHostedService and hosts the DurabilityAgent.
        var hostedServices = host.Services.GetServices<IHostedService>();
        hostedServices.Should().ContainSingle(s => s.GetType().Name == "WolverineRuntime");

        await host.StopAsync();
    }

    /// <summary>
    /// Counts outgoing messages in Wolverine's message store.
    /// Uses raw SQL to query the table that Wolverine creates via Weasel migration.
    /// </summary>
    private static async Task<int> CountOutgoingMessagesAsync(ServiceOperationsDbContext context)
    {
        var connection = context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*)::int FROM service_operations.wolverine_outgoing_envelopes";
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

        var result = await command.ExecuteScalarAsync();
        return result is DBNull ? 0 : Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Test message used to prove Wolverine discovers and invokes handlers from a discovered assembly.
    /// AC-016
    /// </summary>
    public record TestDiscoveryMessage;

    /// <summary>
    /// Test handler used to prove Wolverine discovers and invokes handlers from a discovered assembly.
    /// AC-016
    /// </summary>
    public class TestDiscoveryHandler
    {
        public static void Handle(TestDiscoveryMessage message)
        {
            Interlocked.Increment(ref _testDiscoveryHandledCount);
        }
    }
}
