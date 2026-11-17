namespace CatCar.Contexts.Communication.Tests.Features.ApprovalLinks;

using CatCar.Contexts.Communication.Domain.ExternalAccessTokens;
using CatCar.Contexts.Communication.Features.ApprovalLinks.SendApprovalRequest;
using CatCar.Contexts.Communication.Integrations;
using CatCar.Contexts.Communication.Notifications;
using CatCar.Contracts.ServiceOperations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RiseOn.RailResult.Upshot;
using Xunit;

/// <summary>
/// Unit tests for <see cref="SendApprovalRequestHandler"/>: the Wolverine event handler that reacts to
/// <see cref="BudgetIssuedIntegrationEvent"/> by issuing an approval-link token and e-mailing the customer.
/// </summary>
public class SendApprovalRequestHandlerTests
{
    private readonly IServiceOperationsAcl _acl = Substitute.For<IServiceOperationsAcl>();
    private readonly IExternalAccessTokenRepository _tokenRepository = Substitute.For<IExternalAccessTokenRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly CapturingLogger<SendApprovalRequestHandler.Marker> _logger = new();
    private readonly IOptions<CommunicationOptions> _options = Microsoft.Extensions.Options.Options.Create(
        new CommunicationOptions { ExternalBaseUrl = "https://catcar.example.com", ApprovalLinkTtlDays = 7 });

    [Fact]
    public async Task Handle_WhenBudgetSnapshotNotFound_ShouldLogStructuredFailureAndThrow()
    {
        var @event = CreateEvent();
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Fail("Orçamento não encontrado."));

        var act = () => SendApprovalRequestHandler.Handle(@event, _acl, _tokenRepository, _emailSender, _options, _logger, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        AssertFailureLog(@event, "BudgetSnapshotUnavailable");
        await _tokenRepository.DidNotReceive().AddAsync(Arg.Any<ExternalAccessToken>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Handle_WhenCustomerEmailMissingOrInvalid_ShouldLogStructuredFailureAndThrow(string? email)
    {
        var @event = CreateEvent();
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Success(CreateSnapshot(@event, email)));

        var act = () => SendApprovalRequestHandler.Handle(@event, _acl, _tokenRepository, _emailSender, _options, _logger, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        AssertFailureLog(@event, "InvalidRecipient");
        await _tokenRepository.DidNotReceive().AddAsync(Arg.Any<ExternalAccessToken>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProviderFails_ShouldLogRedactedFailureAndPropagate()
    {
        var @event = CreateEvent();
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>())
            .Returns(Upshot<BudgetSnapshot>.Success(CreateSnapshot(@event, "cliente@example.com")));
        _emailSender.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("failure for cliente@example.com")));

        var act = () => SendApprovalRequestHandler.Handle(@event, _acl, _tokenRepository, _emailSender, _options, _logger, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        AssertFailureLog(@event, "EmailProviderFailure");
        _logger.Entries.Should().NotContain(e => e.Message.Contains("cliente@example.com", StringComparison.Ordinal)
            || e.Message.Contains("approval-links", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_WithValidSnapshotAndEmail_ShouldIssueTokenPersistAndSendEmail()
    {
        var @event = CreateEvent();
        var snapshot = CreateSnapshot(@event, customerEmail: "cliente@example.com");
        _acl.GetBudgetSnapshotAsync(@event.BudgetId, Arg.Any<CancellationToken>()).Returns(Upshot<BudgetSnapshot>.Success(snapshot));

        await SendApprovalRequestHandler.Handle(@event, _acl, _tokenRepository, _emailSender, _options, _logger, CancellationToken.None);

        await _tokenRepository.Received(1).AddAsync(
            Arg.Is<ExternalAccessToken>(t => t.BudgetId == @event.BudgetId), Arg.Any<CancellationToken>());
        await _tokenRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.ToAddress == "cliente@example.com" && m.Body.Contains("https://catcar.example.com/api/v1/communication/approval-links/", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    private void AssertFailureLog(BudgetIssuedIntegrationEvent @event, string failureCode)
    {
        var entry = _logger.Entries.Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Message.Should().Contain("WorkOrderIntegrationFailed")
            .And.Contain(@event.WorkOrderId.ToString())
            .And.Contain(@event.BudgetId.ToString())
            .And.Contain(@event.CorrelationId.ToString())
            .And.Contain("Component=Communication")
            .And.Contain("Operation=SendApprovalRequest")
            .And.Contain($"FailureCode={failureCode}");
    }

    private static BudgetIssuedIntegrationEvent CreateEvent() =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 300m, DateTime.UtcNow, Guid.NewGuid());

    private static BudgetSnapshot CreateSnapshot(BudgetIssuedIntegrationEvent @event, string? customerEmail) =>
        new(
            @event.BudgetId,
            @event.WorkOrderId,
            @event.CustomerId,
            "Cliente Teste",
            customerEmail,
            "Active",
            @event.TotalAmount,
            @event.IssuedAt,
            [new CatCar.Contexts.Communication.Integrations.BudgetSnapshotLine("Service", "Troca de óleo", 150m, 1, 150m)]);
}
