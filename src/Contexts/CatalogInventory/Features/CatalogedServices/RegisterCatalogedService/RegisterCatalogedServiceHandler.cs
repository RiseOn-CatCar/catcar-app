namespace CatCar.Contexts.CatalogInventory.Features.CatalogedServices.RegisterCatalogedService;

using CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;
using FluentValidation;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Wolverine handler for <see cref="RegisterCatalogedServiceCommand"/>.
/// </summary>
public static class RegisterCatalogedServiceHandler
{
    public static async Task<Upshot<RegisterCatalogedServiceResult>> Handle(
        RegisterCatalogedServiceCommand command,
        IValidator<RegisterCatalogedServiceCommand> validator,
        ICatalogedServiceRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Upshot<RegisterCatalogedServiceResult>.Fail(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        if (await repository.ExistsByNameAsync(command.Name.Trim(), cancellationToken).ConfigureAwait(false))
            return Upshot<RegisterCatalogedServiceResult>.Fail("Já existe um serviço cadastrado com este nome.");

        var serviceResult = CatalogedService.Register(command.Name, command.Description, command.EstimatedDurationMinutes, command.Price);
        if (serviceResult.IsFailure)
            return Upshot<RegisterCatalogedServiceResult>.Fail(serviceResult.Error);

        var service = serviceResult.Value;

        await repository.AddAsync(service, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Upshot<RegisterCatalogedServiceResult>.Success(
            new RegisterCatalogedServiceResult(service.Id, service.Name, service.Description, service.EstimatedDurationMinutes, service.Price.Amount, service.IsActive));
    }
}
