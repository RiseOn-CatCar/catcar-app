namespace CatCar.Contexts.ServiceOperations.Domain.Budgets;

using CatCar.SharedKernel;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Aggregate root representing the active commercial proposal of services and parts linked to a WorkOrder.
/// Frozen for audit purposes from the moment of issuance. Context: Atendimento/OS (ServiceOperations).
/// Aliases (forbidden per glossary): "cotação dinâmica".
/// AC: orçamento gerado automaticamente; envio ao cliente para aprovação (feature 04 issues it, feature 05
/// records the customer's approval/rejection).
/// </summary>
public sealed class Budget : Entity<Guid>, IAggregateRoot
{
    private readonly List<BudgetLine> _lines = [];

    public Guid WorkOrderId { get; private set; }

    public BudgetStatus Status { get; private set; }

    public DateTime IssuedAt { get; private set; }

    public IReadOnlyList<BudgetLine> Lines => _lines;

    public decimal TotalAmount => _lines.Sum(l => l.LineTotal);

    private Budget(Guid id, Guid workOrderId, DateTime issuedAt)
        : base(id)
    {
        WorkOrderId = workOrderId;
        Status = BudgetStatus.Active;
        IssuedAt = issuedAt;
    }

    /// <summary>
    /// Issues a new budget for a WorkOrder, freezing the service/part lines as an immutable snapshot.
    /// </summary>
    public static Upshot<Budget> Issue(
        Guid workOrderId,
        IReadOnlyList<(BudgetLineType Type, Guid ReferenceId, string Description, decimal UnitPrice, int Quantity)> lines)
    {
        if (workOrderId == Guid.Empty)
            return Upshot<Budget>.Fail("A OS vinculada ao orçamento é obrigatória.");

        if (lines.Count == 0)
            return Upshot<Budget>.Fail("O orçamento precisa conter ao menos uma linha de serviço ou peça.");

        var budget = new Budget(Guid.CreateVersion7(), workOrderId, DateTime.UtcNow);
        foreach (var line in lines)
        {
            budget._lines.Add(new BudgetLine(line.Type, line.ReferenceId, line.Description, line.UnitPrice, line.Quantity));
        }

        return Upshot<Budget>.Success(budget);
    }
}
