namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.UpdateCatalogedService;

using CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="UpdateCatalogedServiceCommand"/>.
/// </summary>
public static class UpdateCatalogedServiceHandler
{
    public static async Task<Upshot<UpdateCatalogedServiceResult>> Handle(
        UpdateCatalogedServiceCommand command,
        IValidator<UpdateCatalogedServiceCommand> validator,
        ICatalogedServiceRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<UpdateCatalogedServiceResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var service = await repository.GetByIdAsync(command.ServiceId, cancellationToken).ConfigureAwait(false);
        if (service is null)
            return Upshot<UpdateCatalogedServiceResult>.Fail("Serviço não encontrado.");

        var updateResult = service.UpdateDetails(command.Name, command.Description, command.EstimatedDurationMinutes, command.Price);
        if (updateResult.IsFailure)
            return Upshot<UpdateCatalogedServiceResult>.Fail(updateResult.Error);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<UpdateCatalogedServiceResult>.Success(
            new UpdateCatalogedServiceResult(service.Id, service.Name, service.Description, service.EstimatedDurationMinutes, service.Price.Amount, service.IsActive));
    }
}
