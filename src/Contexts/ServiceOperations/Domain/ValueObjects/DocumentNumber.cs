namespace CatCar.Contexts.ServiceOperations.Domain.ValueObjects;

using CatCar.SharedKernel;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Type of Brazilian taxpayer document used to identify a <c>Customer</c>.
/// </summary>
public enum DocumentType
{
    Cpf,
    Cnpj
}

/// <summary>
/// Value object representing a Brazilian CPF (individual) or CNPJ (legal entity) document number,
/// used to identify a Customer within the ServiceOperations (Atendimento/OS) context.
/// AC: Identificação do cliente por CPF/CNPJ; Validação de dados sensíveis.
/// </summary>
public sealed class DocumentNumber : ValueObject
{
    /// <summary>
    /// Normalized digits-only representation (11 digits for CPF, 14 for CNPJ).
    /// </summary>
    public string Value { get; }

    public DocumentType Type { get; }

    private DocumentNumber(string value, DocumentType type)
    {
        Value = value;
        Type = type;
    }

    /// <summary>
    /// Parses and validates a CPF/CNPJ, accepting formatted or unformatted input.
    /// The document type is inferred from the digit count (11 = CPF, 14 = CNPJ).
    /// </summary>
    public static Upshot<DocumentNumber> Create(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return Upshot<DocumentNumber>.Fail("O documento (CPF/CNPJ) é obrigatório.");

        var digits = new string(rawValue.Where(char.IsDigit).ToArray());

        return digits.Length switch
        {
            11 when IsValidCpf(digits) => Upshot<DocumentNumber>.Success(new DocumentNumber(digits, DocumentType.Cpf)),
            11 => Upshot<DocumentNumber>.Fail("CPF inválido."),
            14 when IsValidCnpj(digits) => Upshot<DocumentNumber>.Success(new DocumentNumber(digits, DocumentType.Cnpj)),
            14 => Upshot<DocumentNumber>.Fail("CNPJ inválido."),
            _ => Upshot<DocumentNumber>.Fail("O documento deve ser um CPF (11 dígitos) ou CNPJ (14 dígitos) válido.")
        };
    }

    private static bool IsValidCpf(string cpf)
    {
        if (cpf.Distinct().Count() == 1)
            return false;

        var digits = cpf.Select(c => c - '0').ToArray();

        var firstCheck = ComputeCheckDigit(digits[..9], 10);
        if (firstCheck != digits[9])
            return false;

        var secondCheck = ComputeCheckDigit(digits[..10], 11);
        return secondCheck == digits[10];
    }

    private static bool IsValidCnpj(string cnpj)
    {
        if (cnpj.Distinct().Count() == 1)
            return false;

        var digits = cnpj.Select(c => c - '0').ToArray();

        int[] firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] secondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var firstCheck = ComputeWeightedCheckDigit(digits[..12], firstWeights);
        if (firstCheck != digits[12])
            return false;

        var secondCheck = ComputeWeightedCheckDigit(digits[..13], secondWeights);
        return secondCheck == digits[13];
    }

    private static int ComputeCheckDigit(int[] digits, int startingWeight)
    {
        var sum = 0;
        var weight = startingWeight;
        foreach (var digit in digits)
        {
            sum += digit * weight;
            weight--;
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static int ComputeWeightedCheckDigit(int[] digits, int[] weights)
    {
        var sum = digits.Select((d, i) => d * weights[i]).Sum();
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Type == DocumentType.Cpf
        ? $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}"
        : $"{Value[..2]}.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-{Value[12..]}";
}
