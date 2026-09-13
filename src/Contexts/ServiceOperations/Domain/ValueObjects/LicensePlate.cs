namespace CatCar.Contexts.ServiceOperations.Domain.ValueObjects;

using System.Text.RegularExpressions;
using CatCar.SharedKernel;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Value object representing a Brazilian vehicle license plate, in either the legacy format
/// (LLLNNNN, e.g. ABC1234) or the Mercosul format (LLLNLNN, e.g. ABC1D23).
/// AC: Cadastro de veículo (placa); Validação de dados sensíveis (placa de veículo).
/// </summary>
public sealed partial class LicensePlate : ValueObject
{
    private static readonly Regex LegacyFormat = LegacyFormatRegex();
    private static readonly Regex MercosulFormatRegex2 = MercosulFormatRegex();

    /// <summary>
    /// Normalized value, always uppercase and without separators (e.g. "ABC1234").
    /// </summary>
    public string Value { get; }

    private LicensePlate(string value)
    {
        Value = value;
    }

    public static Upshot<LicensePlate> Create(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return Upshot<LicensePlate>.Fail("A placa do veículo é obrigatória.");

        var normalized = rawValue.Trim().ToUpperInvariant().Replace("-", string.Empty, StringComparison.Ordinal);

        if (!LegacyFormat.IsMatch(normalized) && !MercosulFormatRegex2.IsMatch(normalized))
            return Upshot<LicensePlate>.Fail("A placa informada não corresponde a um formato válido (padrão antigo ABC1234 ou Mercosul ABC1D23).");

        return Upshot<LicensePlate>.Success(new LicensePlate(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z]{3}[0-9]{4}$")]
    private static partial Regex LegacyFormatRegex();

    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z][0-9]{2}$")]
    private static partial Regex MercosulFormatRegex();
}
