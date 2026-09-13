namespace CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;

using CatCar.Contexts.CatalogInventory.Domain.ValueObjects;
using CatCar.SharedKernel;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Aggregate root representing a service offered by the workshop (e.g. troca de óleo, alinhamento).
/// AC: CRUD de serviços (feature 03).
/// Aliases (forbidden per glossary): "item de serviço".
/// </summary>
public sealed class CatalogedService : Entity<Guid>, IAggregateRoot
{
    public string Name { get; private set; }

    public string Description { get; private set; }

    public int EstimatedDurationMinutes { get; private set; }

    public Money Price { get; private set; }

    public bool IsActive { get; private set; }

    private CatalogedService(Guid id, string name, string description, int estimatedDurationMinutes, Money price)
        : base(id)
    {
        Name = name;
        Description = description;
        EstimatedDurationMinutes = estimatedDurationMinutes;
        Price = price;
        IsActive = true;
    }

    /// <summary>
    /// Registers a new cataloged service.
    /// </summary>
    public static Upshot<CatalogedService> Register(string? name, string? description, int estimatedDurationMinutes, decimal priceAmount)
    {
        var validation = ValidateDetails(name, description, estimatedDurationMinutes);
        if (validation.IsFailure)
            return Upshot<CatalogedService>.Fail(validation.Error);

        var priceResult = Money.Create(priceAmount);
        if (priceResult.IsFailure)
            return Upshot<CatalogedService>.Fail(priceResult.Error);

        var service = new CatalogedService(Guid.CreateVersion7(), name!.Trim(), description!.Trim(), estimatedDurationMinutes, priceResult.Value);
        return Upshot<CatalogedService>.Success(service);
    }

    /// <summary>
    /// Updates the descriptive and pricing details of the service.
    /// </summary>
    public Upshot UpdateDetails(string? name, string? description, int estimatedDurationMinutes, decimal priceAmount)
    {
        var validation = ValidateDetails(name, description, estimatedDurationMinutes);
        if (validation.IsFailure)
            return Upshot.Fail(validation.Error);

        var priceResult = Money.Create(priceAmount);
        if (priceResult.IsFailure)
            return Upshot.Fail(priceResult.Error);

        Name = name!.Trim();
        Description = description!.Trim();
        EstimatedDurationMinutes = estimatedDurationMinutes;
        Price = priceResult.Value;
        return Upshot.Success();
    }

    /// <summary>
    /// Activates a previously deactivated service, making it available for new work orders.
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            throw new BusinessRuleViolatedException("O serviço já está ativo.");

        IsActive = true;
    }

    /// <summary>
    /// Deactivates the service, preventing it from being offered in new work orders.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            throw new BusinessRuleViolatedException("O serviço já está inativo.");

        IsActive = false;
    }

    private static Upshot ValidateDetails(string? name, string? description, int estimatedDurationMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Upshot.Fail("O nome do serviço é obrigatório.");

        if (name.Trim().Length > 150)
            return Upshot.Fail("O nome do serviço deve ter no máximo 150 caracteres.");

        if (string.IsNullOrWhiteSpace(description))
            return Upshot.Fail("A descrição do serviço é obrigatória.");

        if (description.Trim().Length > 500)
            return Upshot.Fail("A descrição do serviço deve ter no máximo 500 caracteres.");

        if (estimatedDurationMinutes <= 0)
            return Upshot.Fail("A duração estimada deve ser maior que zero minutos.");

        return Upshot.Success();
    }
}
