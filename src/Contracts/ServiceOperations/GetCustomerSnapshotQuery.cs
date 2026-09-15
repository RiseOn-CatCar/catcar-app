namespace CatCar.Contracts.ServiceOperations;

/// <summary>
/// Published-language query used by other bounded contexts (e.g. Communication) to obtain a read-only
/// snapshot of a Customer to address notification emails.
/// </summary>
public sealed record GetCustomerSnapshotQuery(Guid CustomerId);
