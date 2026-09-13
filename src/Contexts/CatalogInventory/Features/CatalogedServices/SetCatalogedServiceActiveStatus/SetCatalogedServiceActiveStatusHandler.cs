namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.SetCatalogedServiceActiveStatus;

using CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;
using CatCar.SharedKernel;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="SetCatalogedServiceActiveStatusCommand"/>.
/// </summary>
public static class SetCatalogedServiceActiveStatusHandler
{
    public static async Task<Upshot<SetCatalogedServiceActiveStatusResult>> Handle(
        SetCatalogedServiceActiveStatusCommand command,
        IValidator<SetCatalogedServiceActiveStatusCommand> validator,
        ICatalogedServiceRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<SetCatalogedServiceActiveStatusResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var service = await repository.GetByIdAsync(command.ServiceId, cancellationToken).ConfigureAwait(false);
        if (service is null)
            return Upshot<SetCatalogedServiceActiveStatusResult>.Fail("Serviço não encontrado.");

        try
        {
            if (command.IsActive)
                service.Activate();
            else
                service.Deactivate();
        }
        catch (BusinessRuleViolatedException ex)
        {
            return Upshot<SetCatalogedServiceActiveStatusResult>.Fail(ex.Message);
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<SetCatalogedServiceActiveStatusResult>.Success(new SetCatalogedServiceActiveStatusResult(service.Id, service.IsActive));
    }
}
