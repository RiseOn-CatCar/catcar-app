namespace CatCar.Contexts.ServiceOperations.Domain.Vehicles;

using CatCar.Contexts.ServiceOperations.Domain.ValueObjects;
using CatCar.SharedKernel;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Aggregate root representing a customer's vehicle, identified by plate, make/model and year.
/// Context: Atendimento/OS (ServiceOperations). References the owning customer by <see cref="CustomerId"/> only.
/// Aliases (forbidden per glossary): "carro", "automóvel".
/// AC: Cadastro de veículo (placa, marca, modelo, ano); CRUD de veículos (feature 04).
/// </summary>
public sealed class Vehicle : Entity<Guid>, IAggregateRoot
{
    public Guid CustomerId { get; private set; }

    public LicensePlate Plate { get; private set; }

    public string Brand { get; private set; }

    public string Model { get; private set; }

    public int ManufactureYear { get; private set; }

    public bool IsActive { get; private set; }

    private Vehicle(Guid id, Guid customerId, LicensePlate plate, string brand, string model, int manufactureYear)
        : base(id)
    {
        CustomerId = customerId;
        Plate = plate;
        Brand = brand;
        Model = model;
        ManufactureYear = manufactureYear;
        IsActive = true;
    }

    public static Upshot<Vehicle> Register(Guid customerId, string? plate, string? brand, string? model, int manufactureYear)
    {
        if (customerId == Guid.Empty)
            return Upshot<Vehicle>.Fail("O cliente proprietário do veículo é obrigatório.");

        var plateResult = LicensePlate.Create(plate);
        if (plateResult.IsFailure)
            return Upshot<Vehicle>.Fail(plateResult.Error);

        var validation = ValidateDetails(brand, model, manufactureYear);
        if (validation.IsFailure)
            return Upshot<Vehicle>.Fail(validation.Error);

        return Upshot<Vehicle>.Success(new Vehicle(
            Guid.CreateVersion7(),
            customerId,
            plateResult.Value,
            brand!.Trim(),
            model!.Trim(),
            manufactureYear));
    }

    public Upshot UpdateDetails(string? brand, string? model, int manufactureYear)
    {
        var validation = ValidateDetails(brand, model, manufactureYear);
        if (validation.IsFailure)
            return Upshot.Fail(validation.Error);

        Brand = brand!.Trim();
        Model = model!.Trim();
        ManufactureYear = manufactureYear;
        return Upshot.Success();
    }

    public void SetActiveStatus(bool isActive) => IsActive = isActive;

    private static Upshot ValidateDetails(string? brand, string? model, int manufactureYear)
    {
        if (string.IsNullOrWhiteSpace(brand))
            return Upshot.Fail("A marca do veículo é obrigatória.");

        if (brand.Trim().Length > 60)
            return Upshot.Fail("A marca deve ter no máximo 60 caracteres.");

        if (string.IsNullOrWhiteSpace(model))
            return Upshot.Fail("O modelo do veículo é obrigatório.");

        if (model.Trim().Length > 60)
            return Upshot.Fail("O modelo deve ter no máximo 60 caracteres.");

        var currentYear = DateTime.UtcNow.Year;
        if (manufactureYear < 1950 || manufactureYear > currentYear + 1)
            return Upshot.Fail($"O ano de fabricação deve estar entre 1950 e {currentYear + 1}.");

        return Upshot.Success();
    }
}
