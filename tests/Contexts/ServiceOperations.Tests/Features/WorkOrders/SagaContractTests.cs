namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using System.Text.Json;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.Saga;
using FluentAssertions;
using Wolverine.Attributes;
using Xunit;

public sealed class SagaContractTests
{
    [Theory]
    [InlineData(typeof(RequestBudget), "catcar.os.request-budget.v1", "WorkOrderId,CustomerId,CustomerName,CustomerEmail,Lines,RequestedAt,CorrelationId")]
    [InlineData(typeof(CancelBudget), "catcar.os.cancel-budget.v1", "WorkOrderId,BudgetId,Reason,CorrelationId")]
    [InlineData(typeof(RequestPayment), "catcar.os.request-payment.v1", "WorkOrderId,BudgetId,Amount,CustomerEmail,CorrelationId")]
    [InlineData(typeof(RefundPayment), "catcar.os.refund-payment.v1", "WorkOrderId,BudgetId,Reason,CorrelationId")]
    [InlineData(typeof(ReserveParts), "catcar.os.reserve-parts.v1", "WorkOrderId,Parts,CorrelationId")]
    [InlineData(typeof(ReleaseParts), "catcar.os.release-parts.v1", "WorkOrderId,Reason,CorrelationId")]
    [InlineData(typeof(StartExecution), "catcar.os.start-execution.v1", "WorkOrderId,Services,Parts,CorrelationId")]
    [InlineData(typeof(WorkOrderStatusChanged), "catcar.os.work-order-status-changed.v1", "WorkOrderId,CustomerId,CustomerName,CustomerEmail,PreviousStatus,NewStatus,ChangedAt,CorrelationId,EventId")]
    [InlineData(typeof(BudgetIssued), "catcar.billing.budget-issued.v1", "WorkOrderId,BudgetId,TotalAmount,IssuedAt,CorrelationId")]
    [InlineData(typeof(BudgetApproved), "catcar.billing.budget-approved.v1", "WorkOrderId,BudgetId,TotalAmount,ApprovedAt,CorrelationId")]
    [InlineData(typeof(BudgetRejected), "catcar.billing.budget-rejected.v1", "WorkOrderId,BudgetId,Reason,RejectedAt,CorrelationId")]
    [InlineData(typeof(PaymentConfirmed), "catcar.billing.payment-confirmed.v1", "WorkOrderId,BudgetId,PaymentId,Amount,ConfirmedAt,CorrelationId")]
    [InlineData(typeof(PaymentFailed), "catcar.billing.payment-failed.v1", "WorkOrderId,BudgetId,Reason,FailedAt,CorrelationId")]
    [InlineData(typeof(PartsReserved), "catcar.execution.parts-reserved.v1", "WorkOrderId,ReservedAt,CorrelationId")]
    [InlineData(typeof(PartsReservationFailed), "catcar.execution.parts-reservation-failed.v1", "WorkOrderId,FailureCode,FailedAt,CorrelationId")]
    [InlineData(typeof(ExecutionCompleted), "catcar.execution.execution-completed.v1", "WorkOrderId,CompletedAt,CorrelationId")]
    [InlineData(typeof(ExecutionFailed), "catcar.execution.execution-failed.v1", "WorkOrderId,Reason,FailedAt,CorrelationId")]
    public void Messages_HaveStableIdentityAndJsonFields(Type type, string identity, string fields)
    {
        var constructor = type.GetConstructors().Single();
        var arguments = constructor.GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray();
        var message = constructor.Invoke(arguments);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(message, type));
        json.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(fields.Split(','));
        type.GetCustomAttributesData().Single(x => x.AttributeType == typeof(MessageIdentityAttribute)).ConstructorArguments[0].Value.Should().Be(identity);
    }

    [Theory]
    [InlineData(typeof(BudgetLineRequest), "Type,ReferenceId,Description,UnitPrice,Quantity")]
    [InlineData(typeof(PartQuantity), "InventoryItemId,Quantity")]
    [InlineData(typeof(ServiceQuantity), "CatalogedServiceId,Description,Quantity")]
    public void NestedMessages_HaveStableJsonFields(Type type, string fields)
    {
        var constructor = type.GetConstructors().Single();
        var arguments = constructor.GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray();
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(constructor.Invoke(arguments), type));
        json.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(fields.Split(','));
    }
}
