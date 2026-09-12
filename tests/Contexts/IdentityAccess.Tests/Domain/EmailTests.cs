namespace CatCar.Contexts.IdentityAccess.Tests.Domain;

using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using FluentAssertions;
using Xunit;

public class EmailTests
{
    [Theory]
    [InlineData("admin@catcar.dev.br")]
    [InlineData("Recepcao@CatCar.com.br")]
    [InlineData("tecnico.oficina@catcar.io")]
    public void Create_WithValidEmail_ShouldSucceedAndNormalizeToLowerCase(string rawEmail)
    {
        var result = Email.Create(rawEmail);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(rawEmail.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nao-e-email")]
    [InlineData("faltando-arroba.com")]
    [InlineData("@sem-usuario.com")]
    public void Create_WithInvalidEmail_ShouldFail(string? rawEmail)
    {
        var result = Email.Create(rawEmail);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void TwoEmails_WithSameValue_ShouldBeEqual()
    {
        var first = Email.Create("admin@catcar.dev.br").Value;
        var second = Email.Create("ADMIN@CatCar.dev.br").Value;

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }
}
