namespace CatCar.Contexts.ServiceOperations.Tests.Domain;

using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using FluentAssertions;
using Xunit;

public class BudgetTests
{
    private static readonly Guid WorkOrderId = Guid.CreateVersion7();

    [Fact]
    public void Issue_WithValidLines_ShouldSucceedAsActiveAndComputeTotal()
    {
        var lines = new List<(BudgetLineType, Guid, string, decimal, int)>
        {
            (BudgetLineType.Service, Guid.CreateVersion7(), "Troca de óleo", 150m, 1),
            (BudgetLineType.Part, Guid.CreateVersion7(), "Filtro de óleo", 40m, 2),
        };

        var result = Budget.Issue(WorkOrderId, lines);

        result.IsSuccess.Should().BeTrue();
        result.Value.WorkOrderId.Should().Be(WorkOrderId);
        result.Value.Status.Should().Be(BudgetStatus.Active);
        result.Value.Lines.Should().HaveCount(2);
        result.Value.TotalAmount.Should().Be(230m);
    }

    [Fact]
    public void Issue_WithoutWorkOrderId_ShouldFail()
    {
        var lines = new List<(BudgetLineType, Guid, string, decimal, int)>
        {
            (BudgetLineType.Service, Guid.CreateVersion7(), "Troca de óleo", 150m, 1),
        };

        var result = Budget.Issue(Guid.Empty, lines);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Issue_WithoutLines_ShouldFail()
    {
        var result = Budget.Issue(WorkOrderId, []);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Issue_ShouldAssignUniqueIdsToEachLine()
    {
        var lines = new List<(BudgetLineType, Guid, string, decimal, int)>
        {
            (BudgetLineType.Service, Guid.CreateVersion7(), "Troca de óleo", 150m, 1),
            (BudgetLineType.Part, Guid.CreateVersion7(), "Filtro de óleo", 40m, 2),
        };

        var budget = Budget.Issue(WorkOrderId, lines).Value;

        budget.Lines.Select(l => l.Id).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public void Approve_WhenActive_ShouldSucceedAndTransitionToApproved()
    {
        var budget = CreateActiveBudget();

        var result = budget.Approve();

        result.IsSuccess.Should().BeTrue();
        budget.Status.Should().Be(BudgetStatus.Approved);
    }

    [Fact]
    public void Approve_WhenNotActive_ShouldFail()
    {
        var budget = CreateActiveBudget();
        budget.Approve();

        var result = budget.Approve();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reject_WhenActiveWithReason_ShouldSucceedAndTransitionToRejected()
    {
        var budget = CreateActiveBudget();

        var result = budget.Reject("Preço acima do esperado");

        result.IsSuccess.Should().BeTrue();
        budget.Status.Should().Be(BudgetStatus.Rejected);
        budget.RejectionReason.Should().Be("Preço acima do esperado");
    }

    [Fact]
    public void Reject_WithoutReason_ShouldFail()
    {
        var budget = CreateActiveBudget();

        var result = budget.Reject(null);

        result.IsFailure.Should().BeTrue();
        budget.Status.Should().Be(BudgetStatus.Active);
    }

    [Fact]
    public void Reject_WhenNotActive_ShouldFail()
    {
        var budget = CreateActiveBudget();
        budget.Approve();

        var result = budget.Reject("Motivo qualquer");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Replace_WhenActive_ShouldSucceedAndTransitionToReplaced()
    {
        var budget = CreateActiveBudget();

        var result = budget.Replace();

        result.IsSuccess.Should().BeTrue();
        budget.Status.Should().Be(BudgetStatus.Replaced);
    }

    [Fact]
    public void Replace_WhenApproved_ShouldFail()
    {
        var budget = CreateActiveBudget();
        budget.Approve();

        var result = budget.Replace();

        result.IsFailure.Should().BeTrue();
    }

    private static Budget CreateActiveBudget()
    {
        var lines = new List<(BudgetLineType, Guid, string, decimal, int)>
        {
            (BudgetLineType.Service, Guid.CreateVersion7(), "Troca de óleo", 150m, 1),
        };

        return Budget.Issue(WorkOrderId, lines).Value;
    }
}
