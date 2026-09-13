namespace CatCar.Contexts.Communication.Tests.Features.ApprovalLinks;

using CatCar.Contexts.Communication.Domain.ExternalAccessTokens;
using CatCar.Contexts.Communication.Features.ApprovalLinks.GetApprovalLinkDetails;
using CatCar.Contexts.Communication.Integrations;
using FluentAssertions;
using NSubstitute;
using RiseOn.RailResult.Upshot;
using Xunit;

public class GetApprovalLinkDetailsHandlerTests
{
    private readonly IExternalAccessTokenRepository _tokenRepository = Substitute.For<IExternalAccessTokenRepository>();
    private readonly IServiceOperationsAcl _acl = Substitute.For<IServiceOperationsAcl>();

    [Fact]
    public async Task Handle_WithUnknownToken_ShouldFail()
    {
        var query = new GetApprovalLinkDetailsQuery("token-desconhecido");
        _tokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ExternalAccessToken?)null);

        var result = await GetApprovalLinkDetailsHandler.Handle(query, _tokenRepository, _acl, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ShouldFailAndPersistLazyExpiration()
    {
        var (token, rawToken) = ExternalAccessToken.Issue(Guid.CreateVersion7(), TimeSpan.FromMilliseconds(1));
        await Task.Delay(10);
        var query = new GetApprovalLinkDetailsQuery(rawToken);
        _tokenRepository.GetByTokenHashAsync(token.TokenHash, Arg.Any<CancellationToken>()).Returns(token);

        var result = await GetApprovalLinkDetailsHandler.Handle(query, _tokenRepository, _acl, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        token.Status.Should().Be(ExternalAccessTokenStatus.Expired);
        await _tokenRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidTokenButSnapshotFailure_ShouldFail()
    {
        var (token, rawToken) = ExternalAccessToken.Issue(Guid.CreateVersion7(), TimeSpan.FromDays(7));
        var query = new GetApprovalLinkDetailsQuery(rawToken);
        _tokenRepository.GetByTokenHashAsync(token.TokenHash, Arg.Any<CancellationToken>()).Returns(token);
        _acl.GetBudgetSnapshotAsync(token.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Fail("Orçamento não encontrado."));

        var result = await GetApprovalLinkDetailsHandler.Handle(query, _tokenRepository, _acl, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidTokenAndSnapshot_ShouldSucceedWithoutConsumingToken()
    {
        var (token, rawToken) = ExternalAccessToken.Issue(Guid.CreateVersion7(), TimeSpan.FromDays(7));
        var query = new GetApprovalLinkDetailsQuery(rawToken);
        _tokenRepository.GetByTokenHashAsync(token.TokenHash, Arg.Any<CancellationToken>()).Returns(token);

        var snapshot = new BudgetSnapshot(
            token.BudgetId, Guid.CreateVersion7(), Guid.CreateVersion7(), "Cliente Teste", "cliente@example.com",
            "Active", 300m, DateTime.UtcNow, [new CatCar.Contexts.Communication.Integrations.BudgetSnapshotLine("Service", "Troca de óleo", 150m, 1, 150m)]);
        _acl.GetBudgetSnapshotAsync(token.BudgetId, Arg.Any<CancellationToken>()).Returns(Upshot<BudgetSnapshot>.Success(snapshot));

        var result = await GetApprovalLinkDetailsHandler.Handle(query, _tokenRepository, _acl, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BudgetId.Should().Be(token.BudgetId);
        result.Value.Lines.Should().HaveCount(1);
        token.Status.Should().Be(ExternalAccessTokenStatus.Active);
    }
}
