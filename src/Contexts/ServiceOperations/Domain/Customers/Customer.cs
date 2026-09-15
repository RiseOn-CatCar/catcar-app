namespace CatCar.Contexts.ServiceOperations.Domain.Customers;

using CatCar.Contexts.ServiceOperations.Domain.ValueObjects;
using CatCar.SharedKernel;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Aggregate root representing a customer (individual or legal entity) contracting services from the workshop.
/// Identified by CPF/CNPJ. Context: Atendimento/OS (ServiceOperations).
/// Aliases (forbidden per glossary): "usuário", "consumidor".
/// AC: Identificação do cliente por CPF/CNPJ; CRUD de clientes (feature 04).
/// </summary>
public sealed class Customer : Entity<Guid>, IAggregateRoot
{
    public DocumentNumber Document { get; private set; }

    public string Name { get; private set; }

    public string Phone { get; private set; }

    public string? Email { get; private set; }

    public bool IsActive { get; private set; }

    private Customer(Guid id, DocumentNumber document, string name, string phone, string? email)
        : base(id)
    {
        Document = document;
        Name = name;
        Phone = phone;
        Email = email;
        IsActive = true;
    }

#pragma warning disable CS8618
    private Customer()
        : base(Guid.Empty)
    {
    }
#pragma warning restore CS8618

    /// <summary>
    /// Registers a new customer identified by CPF/CNPJ.
    /// </summary>
    public static Upshot<Customer> Register(string? document, string? name, string? phone, string? email)
    {
        var documentResult = DocumentNumber.Create(document);
        if (documentResult.IsFailure)
            return Upshot<Customer>.Fail(documentResult.Error);

        var validation = ValidateDetails(name, phone, email);
        if (validation.IsFailure)
            return Upshot<Customer>.Fail(validation.Error);

        return Upshot<Customer>.Success(new Customer(
            Guid.CreateVersion7(),
            documentResult.Value,
            name!.Trim(),
            phone!.Trim(),
            NormalizeEmail(email)));
    }

    /// <summary>
    /// Updates the customer's contact details. The document number is immutable once registered.
    /// </summary>
    public Upshot UpdateContactDetails(string? name, string? phone, string? email)
    {
        var validation = ValidateDetails(name, phone, email);
        if (validation.IsFailure)
            return Upshot.Fail(validation.Error);

        Name = name!.Trim();
        Phone = phone!.Trim();
        Email = NormalizeEmail(email);
        return Upshot.Success();
    }

    public void SetActiveStatus(bool isActive) => IsActive = isActive;

    private static Upshot ValidateDetails(string? name, string? phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Upshot.Fail("O nome do cliente é obrigatório.");

        if (name.Trim().Length > 150)
            return Upshot.Fail("O nome do cliente deve ter no máximo 150 caracteres.");

        if (string.IsNullOrWhiteSpace(phone))
            return Upshot.Fail("O telefone do cliente é obrigatório.");

        if (phone.Trim().Length > 20)
            return Upshot.Fail("O telefone deve ter no máximo 20 caracteres.");

        if (!string.IsNullOrWhiteSpace(email) && email.Trim().Length > 200)
            return Upshot.Fail("O e-mail deve ter no máximo 200 caracteres.");

        return Upshot.Success();
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}
