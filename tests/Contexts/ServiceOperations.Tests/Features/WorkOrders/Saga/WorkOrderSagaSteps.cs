namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders.Saga;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.Saga;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Reqnroll;
using Testcontainers.PostgreSql;
using Wolverine.EntityFrameworkCore;

[Binding]
[Scope(Tag = "work-order-saga")]
public sealed class WorkOrderSagaSteps : IAsyncDisposable
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly IDbContextOutbox<ServiceOperationsDbContext> _outbox = Substitute.For<IDbContextOutbox<ServiceOperationsDbContext>>();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().Build();
    private readonly Guid _correlationId = Guid.NewGuid();
    private readonly Guid _budgetId = Guid.NewGuid();
    private ServiceOperationsDbContext _db = null!;
    private WorkOrder _order = null!;
    private WorkOrderSaga _saga = null!;
    private bool _disposed;
    private const decimal Amount = 230m;

    [BeforeScenario]
    public async Task StartDatabase()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<ServiceOperationsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(new AuditInterceptor())
            .Options;
        _db = new ServiceOperationsDbContext(options);
        await _db.Database.EnsureCreatedAsync();
    }

    [AfterScenario]
    public Task StopDatabase() => DisposeAsync().AsTask();

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (_db is not null) await _db.DisposeAsync();
        await _postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Given("a diagnosed work order with a requested service and part")]
    public async Task GivenDiagnosedWorkOrder()
    {
        var customer = Customer.Register("52998224725", "Test Customer", "85999990000", "customer@example.com").Value;
        var vehicle = Vehicle.Register(customer.Id, "ABC1234", "Fiat", "Uno", 2015).Value;
        _order = WorkOrder.Open(customer.Id, vehicle.Id, "Engine repair", DateTime.UtcNow).Value;
        _order.AddRequestedService(Guid.NewGuid(), "Repair", 150m, 1).IsSuccess.Should().BeTrue();
        _order.AddRequestedPart(Guid.NewGuid(), "Filter", 40m, 2).IsSuccess.Should().BeTrue();
        _order.StartDiagnosis(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        _db.Customers.Add(customer);
        _db.Vehicles.Add(vehicle);
        _db.WorkOrders.Add(_order);
        await _db.SaveChangesAsync();
        _saga = new WorkOrderSaga { Id = _order.Id, CorrelationId = _correlationId };
    }

    [Given("its budget has been issued")]
    public async Task GivenBudgetIssued() => await _saga.Handle(
        new BudgetIssued(_order.Id, _budgetId, Amount, DateTime.UtcNow, _correlationId), _db, _outbox, CancellationToken.None);

    [When("the budget is approved")]
    public async Task ApproveBudget() => await _saga.Handle(
        new BudgetApproved(_order.Id, _budgetId, Amount, DateTime.UtcNow, _correlationId), _db, _outbox, CancellationToken.None);

    [When("the budget is rejected")]
    public async Task RejectBudget() => await _saga.Handle(
        new BudgetRejected(_order.Id, _budgetId, "Rejected", DateTime.UtcNow, _correlationId), _db, _outbox, CancellationToken.None);

    [When("approval times out")]
    public async Task ApprovalTimeout() => await _saga.Handle(
        new ApprovalTimeout(_order.Id, _correlationId, TimeSpan.Zero), _db, _outbox, CancellationToken.None);

    [When("parts reservation fails")]
    public async Task ReservationFailed() => await _saga.Handle(
        new PartsReservationFailed(_order.Id, "insufficient-stock", DateTime.UtcNow, _correlationId), _db, _outbox, CancellationToken.None);

    [When("the parts are reserved")]
    public async Task PartsReserved() => await _saga.Handle(
        new PartsReserved(_order.Id, DateTime.UtcNow, _correlationId), _db, _outbox, _configuration, CancellationToken.None);

    [When("payment fails")]
    public async Task PaymentFailed() => await _saga.Handle(
        new PaymentFailed(_order.Id, _budgetId, "Declined", DateTime.UtcNow, _correlationId), _db, _outbox, CancellationToken.None);

    [When("payment times out")]
    public async Task PaymentTimeout() => await _saga.Handle(
        new PaymentTimeout(_order.Id, _correlationId, TimeSpan.Zero), _db, _outbox, CancellationToken.None);

    [When("payment is confirmed")]
    [When("payment is confirmed again")]
    public async Task ConfirmPayment() => await _saga.Handle(
        new PaymentConfirmed(_order.Id, _budgetId, "payment-1", Amount, DateTime.UtcNow, _correlationId), _db, _outbox, CancellationToken.None);

    [When("execution is completed")]
    public async Task CompleteExecution() => await _saga.Handle(
        new ExecutionCompleted(_order.Id, DateTime.UtcNow, _correlationId), _db, _outbox, CancellationToken.None);

    [When("execution fails")]
    public async Task FailExecution() => await _saga.Handle(
        new ExecutionFailed(_order.Id, "Failure", DateTime.UtcNow, _correlationId), _db, _outbox, CancellationToken.None);

    [Then("the work order is (.*)")]
    public void WorkOrderStatusIs(string status) => _order.Status.ToString().Should().Be(status);

    [Then("reservation, payment and execution commands were published")]
    public async Task HappyCommandsPublished()
    {
        await _outbox.Received(1).PublishAsync(Arg.Is<ReserveParts>(x => x.WorkOrderId == _order.Id && x.CorrelationId == _correlationId && x.Parts.Count == 1));
        await _outbox.Received(1).PublishAsync(Arg.Is<RequestPayment>(x => x.WorkOrderId == _order.Id && x.BudgetId == _budgetId && x.Amount == Amount));
        await ExecutionRequestedOnce();
    }

    [Then("execution was requested only once")]
    public async Task ExecutionRequestedOnce() => await _outbox.Received(1).PublishAsync(
        Arg.Is<StartExecution>(x => x.WorkOrderId == _order.Id && x.CorrelationId == _correlationId));

    [Then("the budget was cancelled")]
    public async Task BudgetCancelled() => await _outbox.Received(1).PublishAsync(
        Arg.Is<CancelBudget>(x => x.WorkOrderId == _order.Id && x.BudgetId == _budgetId && x.CorrelationId == _correlationId));

    [Then("the parts were released")]
    public async Task PartsReleased() => await _outbox.Received(1).PublishAsync(
        Arg.Is<ReleaseParts>(x => x.WorkOrderId == _order.Id && x.CorrelationId == _correlationId));

    [Then("a refund was requested")]
    public async Task RefundRequested() => await _outbox.Received(1).PublishAsync(
        Arg.Is<RefundPayment>(x => x.WorkOrderId == _order.Id && x.BudgetId == _budgetId && x.CorrelationId == _correlationId));

    [Then("no refund was requested")]
    public async Task NoRefund() => await _outbox.DidNotReceive().PublishAsync(Arg.Any<RefundPayment>());

    [Then("no compensation commands were published")]
    public async Task NoCompensation()
    {
        await _outbox.DidNotReceive().PublishAsync(Arg.Any<CancelBudget>());
        await _outbox.DidNotReceive().PublishAsync(Arg.Any<ReleaseParts>());
        await _outbox.DidNotReceive().PublishAsync(Arg.Any<RefundPayment>());
    }
}
