namespace CatCar.Contexts.ServiceOperations.Domain.Customers;

using System.ComponentModel.DataAnnotations;

internal static class CustomerEmailValidation
{
    private static readonly EmailAddressAttribute Validator = new();

    public static bool IsValid(string? email) =>
        !string.IsNullOrWhiteSpace(email) && Validator.IsValid(email);
}
