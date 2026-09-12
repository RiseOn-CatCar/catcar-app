namespace CatCar.Contexts.IdentityAccess.Tests.Features.Authentication;

using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using CatCar.Contexts.IdentityAccess.Features.Authentication.Login;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class LoginHandlerTests
{
    private readonly LoginCommandValidator _validator = new();
    private readonly IAdministrativeUserRepository _repository = Substitute.For<IAdministrativeUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnAccessToken()
    {
        var user = AdministrativeUser.Register("Ana", "ana@catcar.dev.br", "hashed", AdministrativeRole.Administrador).Value;
        var command = new LoginCommand("ana@catcar.dev.br", "senha-correta");

        _repository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("senha-correta", "hashed").Returns(true);
        _tokenGenerator.Generate(user).Returns(new GeneratedToken("token-jwt", DateTime.UtcNow.AddHours(1)));

        var result = await LoginHandler.Handle(command, _validator, _repository, _passwordHasher, _tokenGenerator, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("token-jwt");
        result.Value.Role.Should().Be(nameof(AdministrativeRole.Administrador));
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ShouldFail()
    {
        var command = new LoginCommand("desconhecido@catcar.dev.br", "qualquer-senha");
        _repository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((AdministrativeUser?)null);

        var result = await LoginHandler.Handle(command, _validator, _repository, _passwordHasher, _tokenGenerator, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ShouldFail()
    {
        var user = AdministrativeUser.Register("Ana", "ana@catcar.dev.br", "hashed", AdministrativeRole.Tecnico).Value;
        var command = new LoginCommand("ana@catcar.dev.br", "senha-errada");

        _repository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("senha-errada", "hashed").Returns(false);

        var result = await LoginHandler.Handle(command, _validator, _repository, _passwordHasher, _tokenGenerator, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ShouldFail()
    {
        var user = AdministrativeUser.Register("Ana", "ana@catcar.dev.br", "hashed", AdministrativeRole.Tecnico).Value;
        user.Deactivate();
        var command = new LoginCommand("ana@catcar.dev.br", "senha-correta");

        _repository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("senha-correta", "hashed").Returns(true);

        var result = await LoginHandler.Handle(command, _validator, _repository, _passwordHasher, _tokenGenerator, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithEmptyCredentials_ShouldFailValidationWithoutHittingRepository()
    {
        var command = new LoginCommand(string.Empty, string.Empty);

        var result = await LoginHandler.Handle(command, _validator, _repository, _passwordHasher, _tokenGenerator, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }
}
