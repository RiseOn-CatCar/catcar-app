namespace CatCar.Contexts.IdentityAccess.Tests.Features.AdministrativeUsers;

using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using CatCar.Contexts.IdentityAccess.Features.AdministrativeUsers.RegisterAdministrativeUser;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class RegisterAdministrativeUserHandlerTests
{
    private readonly RegisterAdministrativeUserCommandValidator _validator = new();
    private readonly IAdministrativeUserRepository _repository = Substitute.For<IAdministrativeUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldRegisterUser()
    {
        var command = new RegisterAdministrativeUserCommand("Ana Recepção", "ana@catcar.dev.br", "SenhaForte123", "Recepcionista");

        _repository.ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash("SenhaForte123").Returns("hashed-password");

        var result = await RegisterAdministrativeUserHandler.Handle(command, _validator, _repository, _passwordHasher, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("ana@catcar.dev.br");
        result.Value.Role.Should().Be(nameof(AdministrativeRole.Recepcionista));
        await _repository.Received(1).AddAsync(Arg.Any<AdministrativeUser>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ShouldFailAndNotPersist()
    {
        var command = new RegisterAdministrativeUserCommand("Ana", "ana@catcar.dev.br", "SenhaForte123", "Recepcionista");
        _repository.ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await RegisterAdministrativeUserHandler.Handle(command, _validator, _repository, _passwordHasher, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().AddAsync(Arg.Any<AdministrativeUser>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "ana@catcar.dev.br", "SenhaForte123", "Recepcionista")]
    [InlineData("Ana", "email-invalido", "SenhaForte123", "Recepcionista")]
    [InlineData("Ana", "ana@catcar.dev.br", "123", "Recepcionista")]
    [InlineData("Ana", "ana@catcar.dev.br", "SenhaForte123", "PapelInvalido")]
    public async Task Handle_WithInvalidCommand_ShouldFailValidationWithoutHittingRepository(string name, string email, string password, string role)
    {
        var command = new RegisterAdministrativeUserCommand(name, email, password, role);

        var result = await RegisterAdministrativeUserHandler.Handle(command, _validator, _repository, _passwordHasher, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }
}
