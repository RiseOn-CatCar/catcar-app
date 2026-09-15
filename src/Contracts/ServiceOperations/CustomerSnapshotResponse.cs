namespace CatCar.Contracts.ServiceOperations;

/// <summary>
/// Published-language response to <see cref="GetCustomerSnapshotQuery"/>.
/// </summary>
public sealed record CustomerSnapshotResponse(
    bool Found,
    Guid CustomerId,
    string Name,
    string? Email,
    string? Phone,
    bool IsActive);
