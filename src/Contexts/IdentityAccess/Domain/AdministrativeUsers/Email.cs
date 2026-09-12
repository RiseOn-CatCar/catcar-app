namespace CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;

using System.Text.RegularExpressions;
using CatCar.SharedKernel;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Value object representing a validated administrative login e-mail.
/// </summary>
public sealed partial class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Validates and creates an <see cref="Email"/> value object.
    /// </summary>
    public static Upshot<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Upshot<Email>.Fail("O e-mail é obrigatório.");

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > 256)
            return Upshot<Email>.Fail("O e-mail deve ter no máximo 256 caracteres.");

        if (!EmailFormatRegex().IsMatch(normalized))
            return Upshot<Email>.Fail("O e-mail informado é inválido.");

        return Upshot<Email>.Success(new Email(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailFormatRegex();
}
