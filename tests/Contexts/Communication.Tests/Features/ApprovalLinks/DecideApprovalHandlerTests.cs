namespace CatCar.Contexts.Communication.Tests.Features.ApprovalLinks;

using CatCar.Contexts.Communication.Domain.ExternalAccessTokens;
using CatCar.Contexts.Communication.Features.ApprovalLinks.DecideApproval;
using CatCar.Contexts.Communication.Integrations;
using FluentAssertions;
using NSubstitute;
using RiseOn.RailResult.Upshot;
using Xunit;

public class DecideApprovalHandlerTests
{
    private readonly DecideApprovalCommandValidator _validator = new();
    private readonly IExternalAccessTokenRepository _tokenRepository = Substitute.For<IExternalAccessTokenRepository>();
    private readonly IServiceOperationsAcl _acl = Substitute.For<IServiceOperationsAcl>();

    [Fact]
    public async Task Handle_RejectedWithoutReason_ShouldFailValidationWithoutHittingRepository()
    {
        var command = new DecideApprovalCommand("qualquer-token", Approved: false, Reason: null);

        var result = await DecideApprovalHandler.Handle(command, _validator, _tokenRepository, _acl, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _tokenRepository.DidNotReceive().GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ShouldFail()
    {
        var command = new DecideApprovalCommand("token-desconhecido", Approved: true, Reason: null);
        _tokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ExternalAccessToken?)null);

        var result = await DecideApprovalHandler.Handle(command, _validator, _tokenRepository, _acl, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _acl.DidNotReceive().RecordBudgetDecisionAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ShouldFailAndNotCallAcl()
    {
        var (token, rawToken) = ExternalAccessToken.Issue(Guid.CreateVersion7(), TimeSpan.FromMilliseconds(1));
        await Task.Delay(10);
        var command = new DecideApprovalCommand(rawToken, Approved: true, Reason: null);
        _tokenRepository.GetByTokenHashAsync(token.TokenHash, Arg.Any<CancellationToken>()).Returns(token);

        var result = await DecideApprovalHandler.Handle(command, _validator, _tokenRepository, _acl, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _acl.DidNotReceive().RecordBudgetDecisionAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAclDecisionFails_ShouldNotConsumeToken()
    {
        var (token, rawToken) = ExternalAccessToken.Issue(Guid.CreateVersion7(), TimeSpan.FromDays(7));
        var command = new DecideApprovalCommand(rawToken, Approved: true, Reason: null);
        _tokenRepository.GetByTokenHashAsync(token.TokenHash, Arg.Any<CancellationToken>()).Returns(token);
        _acl.RecordBudgetDecisionAsync(token.BudgetId, true, null, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetDecisionResult>.Fail("Orçamento não está mais ativo."));

        var result = await DecideApprovalHandler.Handle(command, _validator, _tokenRepository, _acl, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        token.Status.Should().Be(ExternalAccessTokenStatus.Active); // Retryable - not consumed on failure.
    }

    [Fact]
    public async Task Handle_WhenAclDecisionSucceeds_ShouldConsumeTokenAndPersist()
    {
        var (token, rawToken) = ExternalAccessToken.Issue(Guid.CreateVersion7(), TimeSpan.FromDays(7));
        var command = new DecideApprovalCommand(rawToken, Approved: true, Reason: null);
        _tokenRepository.GetByTokenHashAsync(token.TokenHash, Arg.Any<CancellationToken>()).Returns(token);

        var decisionResult = new BudgetDecisionResult(token.BudgetId, Guid.CreateVersion7(), "Approved", "InExecution");
        _acl.RecordBudgetDecisionAsync(token.BudgetId, true, null, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetDecisionResult>.Success(decisionResult));

        var result = await DecideApprovalHandler.Handle(command, _validator, _tokenRepository, _acl, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BudgetStatus.Should().Be("Approved");
        token.Status.Should().Be(ExternalAccessTokenStatus.Consumed);
        await _tokenRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
