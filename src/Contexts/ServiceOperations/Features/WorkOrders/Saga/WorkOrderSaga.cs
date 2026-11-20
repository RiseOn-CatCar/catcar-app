namespace CatCar.Contexts.ServiceOperations.Features.WorkOrders.Saga;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RiseOn.RailResult.Upshot;
using Wolverine;

public sealed class WorkOrderSaga : Wolverine.Saga
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid BudgetId { get; set; }
    public decimal Amount { get; set; }
    public string State { get; set; } = AwaitingBudget;
    public const string AwaitingBudget = "AwaitingBudget";
    public const string AwaitingApproval = "AwaitingApproval";
    public const string AwaitingReservation = "AwaitingReservation";
    public const string AwaitingPayment = "AwaitingPayment";

    public static void NotFound(BudgetIssued message, ILogger<WorkOrderSaga> logger) => Missing(logger, message.WorkOrderId);
    public static void NotFound(BudgetApproved message, ILogger<WorkOrderSaga> logger) => Missing(logger, message.WorkOrderId);
    public static void NotFound(BudgetRejected message, ILogger<WorkOrderSaga> logger) => Missing(logger, message.WorkOrderId);
    public static void NotFound(PartsReserved message, ILogger<WorkOrderSaga> logger) => Missing(logger, message.WorkOrderId);
    public static void NotFound(PartsReservationFailed message, ILogger<WorkOrderSaga> logger) => Missing(logger, message.WorkOrderId);
    public static void NotFound(PaymentConfirmed message, ILogger<WorkOrderSaga> logger) => Missing(logger, message.WorkOrderId);
    public static void NotFound(PaymentFailed message, ILogger<WorkOrderSaga> logger) => Missing(logger, message.WorkOrderId);
    public static void NotFound(ExecutionCompleted message, ILogger<WorkOrderSaga> logger) => Missing(logger, message.WorkOrderId);
    public static void NotFound(ExecutionFailed message, ILogger<WorkOrderSaga> logger) => Missing(logger, message.WorkOrderId);
    public static void NotFound(ApprovalTimeout message, ILogger<WorkOrderSaga> logger) => Missing(logger, message.WorkOrderId);
    public static void NotFound(PaymentTimeout message, ILogger<WorkOrderSaga> logger) => Missing(logger, message.WorkOrderId);
    private static readonly Action<ILogger, Guid, Exception?> MissingLog = LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(1, "SagaNotFound"), "No work order saga exists for {WorkOrderId}");
    private static void Missing(ILogger logger, Guid id) => MissingLog(logger, id, null);
    public const string Executing = "Executing";

    private bool Matches(Guid correlationId, string state) => CorrelationId == correlationId && State == state;
    private bool Matches(Guid correlationId, Guid budgetId, string state) => Matches(correlationId, state) && BudgetId == budgetId;

    public async Task Handle(BudgetIssued message, ServiceOperationsDbContext db, IMessageBus outbox, CancellationToken ct)
    {
        if (!Matches(message.CorrelationId, AwaitingBudget)) return;
        var order = await Order(db, ct).ConfigureAwait(false);
        var previous = order.Status;
        Check(order.MarkBudgetIssued(message.BudgetId, message.IssuedAt));
        BudgetId = message.BudgetId;
        Amount = message.TotalAmount;
        State = AwaitingApproval;
        await PublishStatus(db, outbox, order, previous, message.IssuedAt, ct).ConfigureAwait(false);
    }

    public async Task Handle(BudgetApproved message, ServiceOperationsDbContext db, IMessageBus outbox, CancellationToken ct)
    {
        if (!Matches(message.CorrelationId, message.BudgetId, AwaitingApproval) || message.TotalAmount != Amount) return;
        var order = await Order(db, ct).ConfigureAwait(false);
        Check(order.RecordApproval(true, message.ApprovedAt));
        State = AwaitingReservation;
        await outbox.PublishAsync(new ReserveParts(Id, Parts(order), CorrelationId)).ConfigureAwait(false);
    }

    public async Task Handle(BudgetRejected message, ServiceOperationsDbContext db, IMessageBus outbox, CancellationToken ct)
    {
        if (!Matches(message.CorrelationId, message.BudgetId, AwaitingApproval)) return;
        var order = await Order(db, ct).ConfigureAwait(false);
        var previous = order.Status;
        Check(order.RecordApproval(false, message.RejectedAt));
        MarkCompleted();
        await PublishStatus(db, outbox, order, previous, message.RejectedAt, ct).ConfigureAwait(false);
    }

    public async Task Handle(ApprovalTimeout message, ServiceOperationsDbContext db, IMessageBus outbox, CancellationToken ct)
    {
        if (!Matches(message.CorrelationId, AwaitingApproval)) return;
        await Cancel(db, outbox, DateTime.UtcNow, "Approval timed out", false, false, ct).ConfigureAwait(false);
    }

    public async Task Handle(PartsReservationFailed message, ServiceOperationsDbContext db, IMessageBus outbox, CancellationToken ct)
    {
        if (!Matches(message.CorrelationId, AwaitingReservation)) return;
        var order = await Order(db, ct).ConfigureAwait(false);
        var previous = order.Status;
        Check(order.RecordInventoryReservationFailure(BudgetId, message.FailureCode, message.FailedAt));
        MarkCompleted();
        await outbox.PublishAsync(new CancelBudget(Id, BudgetId, message.FailureCode, CorrelationId)).ConfigureAwait(false);
        await PublishStatus(db, outbox, order, previous, message.FailedAt, ct).ConfigureAwait(false);
    }

    public async Task Handle(PartsReserved message, ServiceOperationsDbContext db, IMessageBus outbox, IConfiguration configuration, CancellationToken ct)
    {
        if (!Matches(message.CorrelationId, AwaitingReservation)) return;
        var order = await Order(db, ct).ConfigureAwait(false);
        var customer = await db.Customers.SingleAsync(x => x.Id == order.CustomerId, ct).ConfigureAwait(false);
        var previous = order.Status;
        Check(order.AwaitPayment(message.ReservedAt));
        State = AwaitingPayment;
        await outbox.PublishAsync(new RequestPayment(Id, BudgetId, Amount, customer.Email!, CorrelationId)).ConfigureAwait(false);
        await outbox.ScheduleAsync(new PaymentTimeout(Id, CorrelationId, Timeout(configuration, "PaymentTimeout", 24)), Timeout(configuration, "PaymentTimeout", 24)).ConfigureAwait(false);
        await PublishStatus(db, outbox, order, previous, message.ReservedAt, ct).ConfigureAwait(false);
    }

    public async Task Handle(PaymentFailed message, ServiceOperationsDbContext db, IMessageBus outbox, CancellationToken ct)
    {
        if (!Matches(message.CorrelationId, message.BudgetId, AwaitingPayment)) return;
        await Cancel(db, outbox, message.FailedAt, message.Reason, true, false, ct).ConfigureAwait(false);
    }

    public async Task Handle(PaymentTimeout message, ServiceOperationsDbContext db, IMessageBus outbox, CancellationToken ct)
    {
        if (!Matches(message.CorrelationId, AwaitingPayment)) return;
        await Cancel(db, outbox, DateTime.UtcNow, "Payment timed out", true, false, ct).ConfigureAwait(false);
    }

    public async Task Handle(PaymentConfirmed message, ServiceOperationsDbContext db, IMessageBus outbox, CancellationToken ct)
    {
        if (!Matches(message.CorrelationId, message.BudgetId, AwaitingPayment) || message.Amount != Amount) return;
        var order = await Order(db, ct).ConfigureAwait(false);
        var previous = order.Status;
        Check(order.StartExecution(message.ConfirmedAt));
        State = Executing;
        await outbox.PublishAsync(new StartExecution(Id, order.RequestedServices.Select(x => new ServiceQuantity(x.CatalogedServiceId, x.Description, x.Quantity)).ToArray(), Parts(order), CorrelationId)).ConfigureAwait(false);
        await PublishStatus(db, outbox, order, previous, message.ConfirmedAt, ct).ConfigureAwait(false);
    }

    public async Task Handle(ExecutionFailed message, ServiceOperationsDbContext db, IMessageBus outbox, CancellationToken ct)
    {
        if (!Matches(message.CorrelationId, Executing)) return;
        await Cancel(db, outbox, message.FailedAt, message.Reason, true, true, ct).ConfigureAwait(false);
    }

    public async Task Handle(ExecutionCompleted message, ServiceOperationsDbContext db, IMessageBus outbox, CancellationToken ct)
    {
        if (!Matches(message.CorrelationId, Executing)) return;
        var order = await Order(db, ct).ConfigureAwait(false);
        var previous = order.Status;
        Check(order.Complete(message.CompletedAt));
        MarkCompleted();
        await PublishStatus(db, outbox, order, previous, message.CompletedAt, ct).ConfigureAwait(false);
    }

    private async Task Cancel(ServiceOperationsDbContext db, IMessageBus outbox, DateTime at, string reason, bool release, bool refund, CancellationToken ct)
    {
        var order = await Order(db, ct).ConfigureAwait(false);
        var previous = order.Status;
        Check(order.Cancel(at));
        if (refund) await outbox.PublishAsync(new RefundPayment(Id, BudgetId, reason, CorrelationId)).ConfigureAwait(false);
        if (release) await outbox.PublishAsync(new ReleaseParts(Id, reason, CorrelationId)).ConfigureAwait(false);
        if (!refund) await outbox.PublishAsync(new CancelBudget(Id, BudgetId, reason, CorrelationId)).ConfigureAwait(false);
        MarkCompleted();
        await PublishStatus(db, outbox, order, previous, at, ct).ConfigureAwait(false);
    }

    private Task<WorkOrder> Order(ServiceOperationsDbContext db, CancellationToken ct) => db.WorkOrders.Include(x => x.RequestedServices).Include(x => x.RequestedParts).AsSplitQuery().SingleAsync(x => x.Id == Id, ct);
    private static PartQuantity[] Parts(WorkOrder order) => order.RequestedParts.Select(x => new PartQuantity(x.InventoryItemId, x.Quantity)).ToArray();
    private static void Check(Upshot result) { if (result.IsFailure) throw new InvalidOperationException(result.Error.Message); }
    internal static TimeSpan Timeout(IConfiguration configuration, string key, int hours) => TimeSpan.TryParse(configuration[$"Saga:{key}"], out var value) && value > TimeSpan.Zero ? value : TimeSpan.FromHours(hours);
    private async Task PublishStatus(ServiceOperationsDbContext db, IMessageBus outbox, WorkOrder order, WorkOrderStatus previous, DateTime at, CancellationToken ct)
    {
        if (previous == order.Status) return;
        var customer = await db.Customers.SingleAsync(x => x.Id == order.CustomerId, ct).ConfigureAwait(false);
        await outbox.PublishAsync(new WorkOrderStatusChanged(Id, customer.Id, customer.Name, customer.Email, previous.ToString(), order.Status.ToString(), at, CorrelationId, Guid.NewGuid())).ConfigureAwait(false);
    }
}
