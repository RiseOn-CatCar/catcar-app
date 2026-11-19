namespace CatCar.Contexts.IdentityAccess.Tests.Integration;

using CatCar.Contexts.CatalogInventory.Infrastructure;
using CatCar.Contexts.Communication.Infrastructure;
using CatCar.Contexts.IdentityAccess.Infrastructure;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contracts.CatalogInventory;
using CatCar.Contracts.ServiceOperations;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Wolverine;
using Wolverine.Runtime;
using Xunit;

[Collection("Process environment")]
public sealed class CrossContextRoutingTests
{
    [Fact]
    public async Task LocalKubernetes_AllCrossContextRequestsHaveOneHandlerAndExecute()
    {
        await using var database = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await database.StartAsync();
        using var connectionSetting = new EnvironmentSetting("ConnectionStrings__catcar", database.GetConnectionString());
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("LocalKubernetes");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:catcar"] = database.GetConnectionString(),
                ["Jwt:Secret"] = "test-admin-signing-key-at-least-32-bytes",
                ["CustomerJwt:SigningKey"] = "test-customer-signing-key-at-least-32-bytes",
                ["Communication:MailpitBaseUrl"] = "http://127.0.0.1:8025",
                ["Communication:SenderAddress"] = "noreply@catcar.test",
                ["Communication:ExternalBaseUrl"] = "http://localhost",
            }));
        });
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        foreach (var context in new DbContext[]
        {
            scope.ServiceProvider.GetRequiredService<ServiceOperationsDbContext>(),
            scope.ServiceProvider.GetRequiredService<CatalogInventoryDbContext>(),
            scope.ServiceProvider.GetRequiredService<CommunicationDbContext>(),
            scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>(),
        })
            await context.Database.MigrateAsync();

        var runtime = scope.ServiceProvider.GetRequiredService<IWolverineRuntime>();
        var requestTypes = new[]
        {
            typeof(GetInventoryItemSnapshotQuery), typeof(GetCatalogedServiceSnapshotQuery),
            typeof(GetBudgetSnapshotQuery), typeof(GetCustomerSnapshotQuery),
            typeof(RecordBudgetDecisionCommand), typeof(GetBudgetReservationOutcomesQuery),
        };
        foreach (var type in requestTypes)
        {
            runtime.RoutingFor(type).Routes.Should().ContainSingle($"{type.Name} must have exactly one route");
        }

        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var id = Guid.NewGuid();
        (await bus.InvokeAsync<InventoryItemSnapshotResponse>(new GetInventoryItemSnapshotQuery(id))).Found.Should().BeFalse();
        (await bus.InvokeAsync<CatalogedServiceSnapshotResponse>(new GetCatalogedServiceSnapshotQuery(id))).Found.Should().BeFalse();
        (await bus.InvokeAsync<BudgetSnapshotResponse>(new GetBudgetSnapshotQuery(id))).Found.Should().BeFalse();
        (await bus.InvokeAsync<CustomerSnapshotResponse>(new GetCustomerSnapshotQuery(id))).Found.Should().BeFalse();
        (await bus.InvokeAsync<RecordBudgetDecisionResponse>(new RecordBudgetDecisionCommand(id, true, null, Guid.NewGuid()))).Success.Should().BeFalse();
        (await bus.InvokeAsync<IReadOnlyList<BudgetReservationOutcomeResponse>>(new GetBudgetReservationOutcomesQuery([id]))).Should().BeEmpty();
    }
}

[CollectionDefinition("Process environment", DisableParallelization = true)]
public sealed class ProcessEnvironmentDefinition;

internal sealed class EnvironmentSetting : IDisposable
{
    private readonly string _name;
    private readonly string? _previous;

    public EnvironmentSetting(string name, string value)
    {
        _name = name;
        _previous = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    public void Dispose() => Environment.SetEnvironmentVariable(_name, _previous);
}
