namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using CatCar.Contexts.ServiceOperations;
using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.IssueBudget;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.Saga;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;
using Wolverine.Tracking;
using Xunit;

public sealed class WorkOrderSagaTrackedTests : IntegrationTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Saga_ProcessesPersistedMessages_ThroughTrackedWolverineSession(bool paymentFails)
    {
        var connectionString = ConnectionString;
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:catcar"] = connectionString,
                ["Services:Execution"] = "http://execution-service:8080",
            }))
            .ConfigureServices((context, services) => services.AddServiceOperations(context.Configuration))
            .UseWolverine(options =>
            {
                options.Discovery.IncludeAssembly(typeof(Marker).Assembly);
                options.CodeGeneration.AlwaysUseServiceLocationFor<ServiceOperationsDbContext>();
                options.PersistMessagesWithPostgresql(connectionString);
                options.UseEntityFrameworkCoreTransactions();
                options.Policies.UseDurableLocalQueues();
                options.UseRabbitMq(new Uri("amqp://guest:guest@localhost:5672"))
                    .AutoProvision().UseConventionalRouting();
                options.Policies.UseDurableInboxOnAllListeners();
                options.Policies.UseDurableOutboxOnAllSendingEndpoints();
                options.StubAllExternalTransports();
                options.UseRuntimeCompilation();
            })
            .Build();

        await host.StartAsync();
        try
        {
            var customer = Customer.Register("52998224725", "Test Customer", "85999990000", "customer@example.com").Value;
            var vehicle = Vehicle.Register(customer.Id, "ABC1234", "Fiat", "Uno", 2015).Value;
            var order = WorkOrder.Open(customer.Id, vehicle.Id, "Engine repair", DateTime.UtcNow).Value;
            order.AddRequestedService(Guid.NewGuid(), "Repair", 150m, 1).IsSuccess.Should().BeTrue();
            order.AddRequestedPart(Guid.NewGuid(), "Filter", 40m, 2).IsSuccess.Should().BeTrue();
            order.StartDiagnosis(DateTime.UtcNow).IsSuccess.Should().BeTrue();
            await using (var scope = host.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ServiceOperationsDbContext>();
                db.Customers.Add(customer);
                db.Vehicles.Add(vehicle);
                db.WorkOrders.Add(order);
                await db.SaveChangesAsync();
            }

            var correlationId = Guid.NewGuid();
            var budgetId = Guid.NewGuid();
            const decimal amount = 230m;
            var requested = await host.InvokeMessageAndWaitAsync(new IssueBudgetCommand(order.Id, correlationId));
            var request = requested.Sent.SingleMessage<RequestBudget>();
            request.WorkOrderId.Should().Be(order.Id);
            request.CorrelationId.Should().Be(correlationId);
            request.Lines.Should().HaveCount(2);
            await AssertPersisted(host, order.Id, WorkOrderStatus.InDiagnosis, WorkOrderSaga.AwaitingBudget);

            await host.InvokeMessageAndWaitAsync(new BudgetIssued(order.Id, budgetId, amount, DateTime.UtcNow, correlationId));
            await AssertPersisted(host, order.Id, WorkOrderStatus.AwaitingApproval, WorkOrderSaga.AwaitingApproval);
            var approved = await host.InvokeMessageAndWaitAsync(new BudgetApproved(order.Id, budgetId, amount, DateTime.UtcNow, correlationId));
            await AssertPersisted(host, order.Id, WorkOrderStatus.AwaitingApproval, WorkOrderSaga.AwaitingReservation);
            approved.Sent.SingleMessage<ReserveParts>().Parts.Should().ContainSingle();

            var reserved = await host.InvokeMessageAndWaitAsync(new PartsReserved(order.Id, DateTime.UtcNow, correlationId));
            reserved.Sent.SingleMessage<RequestPayment>().Amount.Should().Be(amount);
            await AssertPersisted(host, order.Id, WorkOrderStatus.AwaitingPayment, WorkOrderSaga.AwaitingPayment);

            if (paymentFails)
            {
                var failed = await host.InvokeMessageAndWaitAsync(new PaymentFailed(order.Id, budgetId, "Declined", DateTime.UtcNow, correlationId));
                failed.Sent.SingleMessage<ReleaseParts>().WorkOrderId.Should().Be(order.Id);
                failed.Sent.SingleMessage<CancelBudget>().BudgetId.Should().Be(budgetId);
                await AssertPersisted(host, order.Id, WorkOrderStatus.Cancelled, null);
            }
            else
            {
                var paid = await host.InvokeMessageAndWaitAsync(new PaymentConfirmed(order.Id, budgetId, "payment-1", amount, DateTime.UtcNow, correlationId));
                paid.Sent.SingleMessage<StartExecution>().WorkOrderId.Should().Be(order.Id);
                await AssertPersisted(host, order.Id, WorkOrderStatus.InExecution, WorkOrderSaga.Executing);
                await host.InvokeMessageAndWaitAsync(new ExecutionCompleted(order.Id, DateTime.UtcNow, correlationId));
                await AssertPersisted(host, order.Id, WorkOrderStatus.Completed, null);
            }
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static async Task AssertPersisted(IHost host, Guid id, WorkOrderStatus status, string? sagaState)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceOperationsDbContext>();
        var order = await db.WorkOrders.AsNoTracking().SingleAsync(x => x.Id == id);
        order.Status.Should().Be(status);
        var saga = await db.Set<WorkOrderSaga>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (sagaState is null)
            saga.Should().BeNull();
        else
        {
            saga.Should().NotBeNull();
            saga!.State.Should().Be(sagaState);
        }
    }
}
