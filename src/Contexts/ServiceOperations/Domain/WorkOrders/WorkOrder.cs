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

    public Guid? ApprovedBudgetId { get; private set; }

    public DateTime? InventoryReservationFailedAt { get; private set; }

    public string? InventoryReservationFailureCode { get; private set; }

    public DateTime? StatusEnteredAt { get; private set; }

    public DateTime? ExecutionStartedAt { get; private set; }

    public double? DiagnosisDurationSeconds { get; private set; }

    public double? ExecutionDurationSeconds { get; private set; }

    public double? FinalizationDurationSeconds { get; private set; }

    public bool HasCompleteStageHistory { get; private set; }

    public DateTime OpenedAt { get; private set; }

    public DateTime? DiagnosisStartedAt { get; private set; }

    public DateTime? BudgetApprovedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public DateTime? DeliveredAt { get; private set; }

    public DateTime LastUpdatedAt { get; private set; }

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
        StatusEnteredAt = openedAt;
        HasCompleteStageHistory = true;
        LastUpdatedAt = openedAt;
    }

    /// <summary>
    /// Opens a new WorkOrder for an identified customer and vehicle. Status starts as Received.
    /// </summary>
    public static Upshot<WorkOrder> Open(Guid customerId, Guid vehicleId, string? initialDescription, DateTime now)
    {
        if (now.Kind != DateTimeKind.Utc)
            return Upshot<WorkOrder>.Fail("The work order timestamp must be UTC.");

        if (customerId == Guid.Empty)
            return Upshot<WorkOrder>.Fail("O cliente é obrigatório para abrir a OS.");

        if (vehicleId == Guid.Empty)
            return Upshot<WorkOrder>.Fail("O veículo é obrigatório para abrir a OS.");

        if (string.IsNullOrWhiteSpace(initialDescription))
            return Upshot<WorkOrder>.Fail("A descrição inicial da OS é obrigatória.");

        if (initialDescription.Trim().Length > 1000)
            return Upshot<WorkOrder>.Fail("A descrição inicial deve ter no máximo 1000 caracteres.");

        return Upshot<WorkOrder>.Success(new WorkOrder(
            Guid.CreateVersion7(), customerId, vehicleId, initialDescription.Trim(), now));
    }

    /// <summary>
    /// Moves the WorkOrder into diagnosis. Idempotent no-op is not allowed - must be Received.
    /// </summary>
    public Upshot StartDiagnosis(DateTime now)
    {
        if (!IsValidTransitionTime(now))
            return Upshot.Fail("The transition timestamp must be UTC and not precede the last update.");

        if (Status != WorkOrderStatus.Received)
            return Upshot.Fail("Só é possível iniciar o diagnóstico de uma OS com status 'Recebida'.");

        ChangeStatus(WorkOrderStatus.InDiagnosis, now);
        DiagnosisStartedAt = now;
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
    public Upshot MarkBudgetIssued(Guid budgetId, DateTime now)
    {
        if (!IsValidTransitionTime(now))
            return Upshot.Fail("The transition timestamp must be UTC and not precede the last update.");

        if (Status != WorkOrderStatus.Received && Status != WorkOrderStatus.InDiagnosis)
            return Upshot.Fail("Só é possível gerar um orçamento para uma OS 'Recebida' ou 'Em diagnóstico'.");

        if (_requestedServices.Count == 0 && _requestedParts.Count == 0)
            return Upshot.Fail("É necessário incluir ao menos um serviço ou peça antes de gerar o orçamento.");

        ChangeStatus(WorkOrderStatus.AwaitingApproval, now);
        ActiveBudgetId = budgetId;
        InventoryReservationFailedAt = null;
        InventoryReservationFailureCode = null;
        return Upshot.Success();
    }

    /// <summary>
    /// Records the customer's decision on the active budget. Rejection returns the WorkOrder to diagnosis
    /// so its requested items can be reassessed and a new budget issued.
    /// </summary>
    public Upshot ValidateApproval(DateTime now)
    {
        if (!IsValidTransitionTime(now))
            return Upshot.Fail("The decision timestamp must be UTC and not precede the last update.");
        if (Status != WorkOrderStatus.AwaitingApproval)
            return Upshot.Fail("Só é possível registrar uma decisão de aprovação para uma OS 'Aguardando aprovação'.");
        if (ActiveBudgetId is null || ApprovedBudgetId == ActiveBudgetId)
            return Upshot.Fail("The active budget is missing or already approved.");
        return Upshot.Success();
    }

    public Upshot RecordApproval(bool approved, DateTime now)
    {
        var validation = ValidateApproval(now);
        if (validation.IsFailure)
            return validation;

        if (approved)
        {
            ApprovedBudgetId = ActiveBudgetId;
            BudgetApprovedAt = now;
            LastUpdatedAt = now;
        }
        else
        {
            ChangeStatus(WorkOrderStatus.InDiagnosis, now);
            ActiveBudgetId = null;
        }
        return Upshot.Success();
    }

    public Upshot ConfirmInventoryReservation(Guid budgetId, DateTime confirmedAt)
    {
        if (!IsValidTransitionTime(confirmedAt))
            return Upshot.Fail("The confirmation timestamp must be UTC and not precede the last update.");

        if (Status != WorkOrderStatus.AwaitingApproval || ActiveBudgetId != budgetId || ApprovedBudgetId != budgetId)
            return Upshot.Fail("The active budget must be approved and awaiting inventory reservation.");

        ChangeStatus(WorkOrderStatus.InExecution, confirmedAt);
        ExecutionStartedAt = confirmedAt;
        return Upshot.Success();
    }

    public Upshot RecordInventoryReservationFailure(Guid budgetId, string failureCode, DateTime failedAt)
    {
        if (Status != WorkOrderStatus.AwaitingApproval || ActiveBudgetId != budgetId || ApprovedBudgetId != budgetId)
            return Upshot.Fail("The active budget must be approved and awaiting inventory reservation.");
        return ApplyReservationFailure(failureCode, failedAt);
    }

    public Upshot ReconcileInventoryReservationFailure(Guid budgetId, string failureCode, DateTime failedAt)
    {
        if (Status != WorkOrderStatus.InExecution || ActiveBudgetId != budgetId || ApprovedBudgetId != budgetId || HasCompleteStageHistory || ExecutionStartedAt is not null)
            return Upshot.Fail("Only a legacy executing work order with the approved budget can be reconciled.");
        return ApplyReservationFailure(failureCode, failedAt);
    }

    private Upshot ApplyReservationFailure(string failureCode, DateTime failedAt)
    {
        if (!IsValidTransitionTime(failedAt))
            return Upshot.Fail("The failure timestamp must be UTC and not precede the last update.");

        if (string.IsNullOrWhiteSpace(failureCode))
            return Upshot.Fail("A reservation failure code is required.");
        ChangeStatus(WorkOrderStatus.InDiagnosis, failedAt);
        ActiveBudgetId = null;
        InventoryReservationFailedAt = failedAt;
        InventoryReservationFailureCode = failureCode;
        return Upshot.Success();
    }

    public Upshot Complete(DateTime now)
    {
        if (!IsValidTransitionTime(now))
            return Upshot.Fail("The transition timestamp must be UTC and not precede the last update.");

        if (Status != WorkOrderStatus.InExecution)
            return Upshot.Fail("Só é possível concluir uma OS com status 'Em execução'.");

        ChangeStatus(WorkOrderStatus.Completed, now);
        CompletedAt = now;
        return Upshot.Success();
    }

    public Upshot Deliver(DateTime now)
    {
        if (!IsValidTransitionTime(now))
            return Upshot.Fail("The transition timestamp must be UTC and not precede the last update.");

        if (Status != WorkOrderStatus.Completed)
            return Upshot.Fail("Só é possível entregar uma OS com status 'Concluída'.");

        ChangeStatus(WorkOrderStatus.Delivered, now);
        DeliveredAt = now;
        return Upshot.Success();
    }

    private bool IsValidTransitionTime(DateTime now)
        => now.Kind == DateTimeKind.Utc && now >= LastUpdatedAt && (StatusEnteredAt is null || now >= StatusEnteredAt.Value);

    private void ChangeStatus(WorkOrderStatus next, DateTime now)
    {
        if (HasCompleteStageHistory && StatusEnteredAt is { } enteredAt)
        {
            var seconds = (now - enteredAt).TotalSeconds;
            switch (Status)
            {
                case WorkOrderStatus.InDiagnosis:
                    DiagnosisDurationSeconds = (DiagnosisDurationSeconds ?? 0) + seconds;
                    break;
                case WorkOrderStatus.InExecution when next == WorkOrderStatus.Completed:
                    ExecutionDurationSeconds = seconds;
                    break;
                case WorkOrderStatus.Completed when next == WorkOrderStatus.Delivered:
                    FinalizationDurationSeconds = seconds;
                    break;
            }
        }
        Status = next;
        StatusEnteredAt = now;
        LastUpdatedAt = now;
    }

    private Upshot EnsureMutableForLineInclusion()
    {
        if (Status != WorkOrderStatus.Received && Status != WorkOrderStatus.InDiagnosis)
            return Upshot.Fail("Só é possível incluir serviços/peças em uma OS 'Recebida' ou 'Em diagnóstico'.");

        return Upshot.Success();
    }
}
