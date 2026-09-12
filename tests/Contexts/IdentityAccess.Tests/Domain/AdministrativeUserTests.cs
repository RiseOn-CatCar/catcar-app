namespace CatCar.Contexts.IdentityAccess.Tests.Domain;

using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using CatCar.SharedKernel;
using FluentAssertions;
using Xunit;

public class AdministrativeUserTests
{
    [Fact]
    public void Register_WithValidData_ShouldSucceedAndBeActive()
    {
        var result = AdministrativeUser.Register("Ana Recepção", "ana@catcar.dev.br", "hashed-password", AdministrativeRole.Recepcionista);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Ana Recepção");
        result.Value.Email.Value.Should().Be("ana@catcar.dev.br");
        result.Value.Role.Should().Be(AdministrativeRole.Recepcionista);
        result.Value.IsActive.Should().BeTrue();
        result.Value.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithoutName_ShouldFail(string? name)
    {
        var result = AdministrativeUser.Register(name, "ana@catcar.dev.br", "hashed-password", AdministrativeRole.Recepcionista);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_WithInvalidEmail_ShouldFail()
    {
        var result = AdministrativeUser.Register("Ana", "email-invalido", "hashed-password", AdministrativeRole.Recepcionista);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Register_WithoutPasswordHash_ShouldFail(string? passwordHash)
    {
        var result = AdministrativeUser.Register("Ana", "ana@catcar.dev.br", passwordHash!, AdministrativeRole.Recepcionista);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldBecomeInactive()
    {
        var user = AdministrativeUser.Register("Ana", "ana@catcar.dev.br", "hash", AdministrativeRole.Tecnico).Value;

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldThrow()
    {
        var user = AdministrativeUser.Register("Ana", "ana@catcar.dev.br", "hash", AdministrativeRole.Tecnico).Value;
        user.Deactivate();

        var act = () => user.Deactivate();

        act.Should().Throw<BusinessRuleViolatedException>();
    }

    [Fact]
    public void ChangeRole_ShouldUpdateRole()
    {
        var user = AdministrativeUser.Register("Ana", "ana@catcar.dev.br", "hash", AdministrativeRole.Recepcionista).Value;

        user.ChangeRole(AdministrativeRole.Administrador);

        user.Role.Should().Be(AdministrativeRole.Administrador);
    }
}
