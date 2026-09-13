namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.GetCatalogedServiceById;

using CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="GetCatalogedServiceByIdQuery"/>.
/// </summary>
public static class GetCatalogedServiceByIdHandler
{
    public static async Task<Upshot<CatalogedServiceDetails>> Handle(
        GetCatalogedServiceByIdQuery query,
        ICatalogedServiceRepository repository,
        CancellationToken cancellationToken)
    {
        var service = await repository.GetByIdAsync(query.ServiceId, cancellationToken).ConfigureAwait(false);
        if (service is null)
            return Upshot<CatalogedServiceDetails>.Fail("Serviço não encontrado.");

        return Upshot<CatalogedServiceDetails>.Success(
            new CatalogedServiceDetails(service.Id, service.Name, service.Description, service.EstimatedDurationMinutes, service.Price.Amount, service.IsActive));
    }
}
