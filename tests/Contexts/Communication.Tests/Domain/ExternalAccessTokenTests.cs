namespace CatCar.Contexts.Communication.Tests.Domain;

using CatCar.Contexts.Communication.Domain.ExternalAccessTokens;
using FluentAssertions;
using Xunit;

public class ExternalAccessTokenTests
{
    private static readonly Guid BudgetId = Guid.CreateVersion7();

    [Fact]
    public void Issue_ShouldCreateActiveTokenAndReturnRawTokenSeparately()
    {
        var (token, rawToken) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromDays(7));

        token.BudgetId.Should().Be(BudgetId);
        token.Status.Should().Be(ExternalAccessTokenStatus.Active);
        rawToken.Should().NotBeNullOrWhiteSpace();
        token.TokenHash.Should().NotBe(rawToken);
    }

    [Fact]
    public void Issue_CalledTwice_ShouldGenerateUniqueRawTokensAndHashes()
    {
        var (tokenA, rawA) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromDays(7));
        var (tokenB, rawB) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromDays(7));

        rawA.Should().NotBe(rawB);
        tokenA.TokenHash.Should().NotBe(tokenB.TokenHash);
    }

    [Fact]
    public void Issue_TokenHash_ShouldMatchComputeHashOfRawToken()
    {
        var (token, rawToken) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromDays(7));

        token.TokenHash.Should().Be(ExternalAccessToken.ComputeHash(rawToken));
    }

    [Fact]
    public void Validate_WithCorrectRawToken_ShouldSucceedAndNotConsume()
    {
        var (token, rawToken) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromDays(7));

        var result = token.Validate(rawToken);

        result.IsSuccess.Should().BeTrue();
        token.Status.Should().Be(ExternalAccessTokenStatus.Active);
    }

    [Fact]
    public void Validate_WithWrongRawToken_ShouldFail()
    {
        var (token, _) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromDays(7));

        var result = token.Validate("token-invalido-completamente-diferente");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullOrEmptyRawToken_ShouldFail()
    {
        var (token, _) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromDays(7));

        token.Validate(null).IsFailure.Should().BeTrue();
        token.Validate(string.Empty).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenExpired_ShouldFailAndLazilyTransitionToExpired()
    {
        var (token, rawToken) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromMilliseconds(1));
        Thread.Sleep(10);

        var result = token.Validate(rawToken);

        result.IsFailure.Should().BeTrue();
        token.Status.Should().Be(ExternalAccessTokenStatus.Expired);
    }

    [Fact]
    public void Validate_WhenAlreadyConsumed_ShouldFail()
    {
        var (token, rawToken) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromDays(7));
        token.Consume();

        var result = token.Validate(rawToken);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Consume_WhenActive_ShouldSucceedAndTransitionToConsumed()
    {
        var (token, _) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromDays(7));

        var result = token.Consume();

        result.IsSuccess.Should().BeTrue();
        token.Status.Should().Be(ExternalAccessTokenStatus.Consumed);
        token.ConsumedAt.Should().NotBeNull();
    }

    [Fact]
    public void Consume_CalledTwice_ShouldFailOnSecondCall_SingleUseEnforced()
    {
        var (token, _) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromDays(7));
        token.Consume();

        var result = token.Consume();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Consume_WhenExpired_ShouldFailAndLazilyTransitionToExpired()
    {
        var (token, _) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromMilliseconds(1));
        Thread.Sleep(10);

        var result = token.Consume();

        result.IsFailure.Should().BeTrue();
        token.Status.Should().Be(ExternalAccessTokenStatus.Expired);
    }

    [Fact]
    public void Expire_WhenActive_ShouldSucceed()
    {
        var (token, _) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromDays(7));

        var result = token.Expire();

        result.IsSuccess.Should().BeTrue();
        token.Status.Should().Be(ExternalAccessTokenStatus.Expired);
    }

    [Fact]
    public void Expire_WhenAlreadyConsumed_ShouldFail()
    {
        var (token, _) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromDays(7));
        token.Consume();

        var result = token.Expire();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Expire_WhenAlreadyExpired_ShouldFail()
    {
        var (token, _) = ExternalAccessToken.Issue(BudgetId, TimeSpan.FromDays(7));
        token.Expire();

        var result = token.Expire();

        result.IsFailure.Should().BeTrue();
    }
}
