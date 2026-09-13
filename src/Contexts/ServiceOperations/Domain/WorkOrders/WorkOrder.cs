namespace CatCar.Contexts.ServiceOperations.Domain.WorkOrders;

using CatCar.SharedKernel;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Aggregate root representing a unit of tracking for work performed on a customer's vehicle (Ordem de Serviço).
/// Context: Atendimento/OS (ServiceOperations). References Customer/Vehicle by ID only.
/// Aliases (forbidden per glossary): "chamado", "ticket".
/// AC-001/AC-002 (event-storming): abertura da OS, inclusão de serviços/peças, geração de orçamento (feature 04).
/// </summary>
public sealed class WorkOrder : Entity<Guid>, IAggregateRoot
{
    private readonly List<RequestedServiceLine> _requestedServices = [];
    private readonly List<RequestedPartLine> _requestedParts = [];

    public Guid CustomerId { get; private set; }

    public Guid VehicleId { get; private set; }

    public string InitialDescription { get; private set; }

    public WorkOrderStatus Status { get; private set; }

    public Guid? ActiveBudgetId { get; private set; }

    public DateTime OpenedAt { get; private set; }

    public IReadOnlyList<RequestedServiceLine> RequestedServices => _requestedServices;

    public IReadOnlyList<RequestedPartLine> RequestedParts => _requestedParts;

    private WorkOrder(Guid id, Guid customerId, Guid vehicleId, string initialDescription, DateTime openedAt)
        : base(id)
    {
        CustomerId = customerId;
        VehicleId = vehicleId;
        InitialDescription = initialDescription;
        Status = WorkOrderStatus.Received;
        OpenedAt = openedAt;
    }

    /// <summary>
    /// Opens a new WorkOrder for an identified customer and vehicle. Status starts as Received.
    /// </summary>
    public static Upshot<WorkOrder> Open(Guid customerId, Guid vehicleId, string? initialDescription)
    {
        if (customerId == Guid.Empty)
            return Upshot<WorkOrder>.Fail("O cliente é obrigatório para abrir a OS.");

        if (vehicleId == Guid.Empty)
            return Upshot<WorkOrder>.Fail("O veículo é obrigatório para abrir a OS.");

        if (string.IsNullOrWhiteSpace(initialDescription))
            return Upshot<WorkOrder>.Fail("A descrição inicial da OS é obrigatória.");

        if (initialDescription.Trim().Length > 1000)
            return Upshot<WorkOrder>.Fail("A descrição inicial deve ter no máximo 1000 caracteres.");

        return Upshot<WorkOrder>.Success(new WorkOrder(
            Guid.CreateVersion7(), customerId, vehicleId, initialDescription.Trim(), DateTime.UtcNow));
    }

    /// <summary>
    /// Moves the WorkOrder into diagnosis. Idempotent no-op is not allowed - must be Received.
    /// </summary>
    public Upshot StartDiagnosis()
    {
        if (Status != WorkOrderStatus.Received)
            return Upshot.Fail("Só é possível iniciar o diagnóstico de uma OS com status 'Recebida'.");

        Status = WorkOrderStatus.InDiagnosis;
        return Upshot.Success();
    }

    /// <summary>
    /// Includes a requested service, referencing the CatalogedService by ID with a price/description snapshot
    /// obtained via the CatalogInventory ACL (AC: inclusão dos serviços solicitados).
    /// </summary>
    public Upshot AddRequestedService(Guid catalogedServiceId, string description, decimal unitPrice, int quantity)
    {
        var guard = EnsureMutableForLineInclusion();
        if (guard.IsFailure)
            return guard;

        if (quantity <= 0)
            return Upshot.Fail("A quantidade do serviço solicitado deve ser maior que zero.");

        _requestedServices.Add(new RequestedServiceLine(catalogedServiceId, description, unitPrice, quantity));
        return Upshot.Success();
    }

    /// <summary>
    /// Includes a requested part/supply, referencing the InventoryItem by ID with a price/description snapshot
    /// obtained via the CatalogInventory ACL (AC: inclusão de peças e insumos necessários).
    /// </summary>
    public Upshot AddRequestedPart(Guid inventoryItemId, string description, decimal unitPrice, int quantity)
    {
        var guard = EnsureMutableForLineInclusion();
        if (guard.IsFailure)
            return guard;

        if (quantity <= 0)
            return Upshot.Fail("A quantidade da peça/insumo solicitada deve ser maior que zero.");

        _requestedParts.Add(new RequestedPartLine(inventoryItemId, description, unitPrice, quantity));
        return Upshot.Success();
    }

    /// <summary>
    /// Marks the WorkOrder as having an active budget issued (AC: orçamento gerado automaticamente com base
    /// nos serviços e peças). Transitions the status to AwaitingApproval, ready to be sent to the customer.
    /// </summary>
    public Upshot MarkBudgetIssued(Guid budgetId)
    {
        if (Status != WorkOrderStatus.Received && Status != WorkOrderStatus.InDiagnosis)
            return Upshot.Fail("Só é possível gerar um orçamento para uma OS 'Recebida' ou 'Em diagnóstico'.");

        if (_requestedServices.Count == 0 && _requestedParts.Count == 0)
            return Upshot.Fail("É necessário incluir ao menos um serviço ou peça antes de gerar o orçamento.");

        Status = WorkOrderStatus.AwaitingApproval;
        ActiveBudgetId = budgetId;
        return Upshot.Success();
    }

    /// <summary>
    /// Records the customer's decision on the active budget, received through the external approval
    /// channel (Communication BC) after the token has already been validated there. Transitions the OS to
    /// InExecution when approved, or to Rejected (terminal for this budget) when rejected
    /// (AC: regra de negocio do status permanece no dominio da OS - feature 05).
    /// </summary>
    public Upshot RecordApproval(bool approved)
    {
        if (Status != WorkOrderStatus.AwaitingApproval)
            return Upshot.Fail("Só é possível registrar uma decisão de aprovação para uma OS 'Aguardando aprovação'.");

        Status = approved ? WorkOrderStatus.InExecution : WorkOrderStatus.Rejected;
        return Upshot.Success();
    }

    private Upshot EnsureMutableForLineInclusion()
    {
        if (Status != WorkOrderStatus.Received && Status != WorkOrderStatus.InDiagnosis)
            return Upshot.Fail("Só é possível incluir serviços/peças em uma OS 'Recebida' ou 'Em diagnóstico'.");

        return Upshot.Success();
    }
}
