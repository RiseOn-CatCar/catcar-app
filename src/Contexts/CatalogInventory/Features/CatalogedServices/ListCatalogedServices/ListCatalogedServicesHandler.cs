namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.ListCatalogedServices;

using CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;

/// <summary>
/// Wolverine handler for <see cref="ListCatalogedServicesQuery"/>.
/// </summary>
public static class ListCatalogedServicesHandler
{
    public static async Task<IReadOnlyList<CatalogedServiceSummary>> Handle(
        ListCatalogedServicesQuery query,
        ICatalogedServiceRepository repository,
        CancellationToken cancellationToken)
    {
        var services = await repository.ListAsync(query.OnlyActive, cancellationToken).ConfigureAwait(false);

        return services
            .Select(s => new CatalogedServiceSummary(s.Id, s.Name, s.EstimatedDurationMinutes, s.Price.Amount, s.IsActive))
            .ToList();
    }
}
