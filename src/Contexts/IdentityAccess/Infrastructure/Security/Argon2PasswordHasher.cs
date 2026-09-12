namespace CatCar.Contexts.IdentityAccess.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;
using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using Konscious.Security.Cryptography;
using RiseOn.AutoInject;

/// <summary>
/// Argon2id-based password hasher. Stateless and safe as a singleton.
/// </summary>
[InjectService(ServiceLifetimeType.Singleton, CollectionName = "IdentityAccess")]
public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Iterations = 4;
    private const int MemorySizeKb = 65536; // 64 MB
    private const int DegreeOfParallelism = 2;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = ComputeHash(password, salt);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.', 2);
        if (parts.Length != 2)
            return false;

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[0]);
            expectedHash = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = ComputeHash(password, salt);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static byte[] ComputeHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySizeKb,
        };

        return argon2.GetBytes(HashSizeBytes);
    }
}
