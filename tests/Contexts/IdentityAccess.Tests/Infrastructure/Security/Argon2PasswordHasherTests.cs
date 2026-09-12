namespace CatCar.Contexts.IdentityAccess.Tests.Infrastructure.Security;

using CatCar.Contexts.IdentityAccess.Infrastructure.Security;
using FluentAssertions;
using Xunit;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_ShouldReturnTrue()
    {
        const string password = "Catcar@Admin123";

        var hash = _hasher.Hash(password);

        _hasher.Verify(password, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        var hash = _hasher.Hash("SenhaCorreta1!");

        _hasher.Verify("SenhaErrada1!", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_CalledTwiceForSamePassword_ShouldProduceDifferentHashes()
    {
        const string password = "MesmaSenha123!";

        var first = _hasher.Hash(password);
        var second = _hasher.Hash(password);

        first.Should().NotBe(second, "each hash uses a random salt");
    }

    [Fact]
    public void Verify_WithMalformedHash_ShouldReturnFalse()
    {
        _hasher.Verify("qualquer-senha", "hash-sem-formato-esperado").Should().BeFalse();
    }
}
