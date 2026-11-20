namespace CatCar.Contexts.ServiceOperations.Tests.Features.WorkOrders;

using System.Net;
using System.Text;
using CatCar.Contexts.ServiceOperations.Integrations;
using FluentAssertions;
using Xunit;

public sealed class CatalogInventoryAclTests
{
    [Fact]
    public async Task GetCatalogedServiceSnapshotAsync_ActiveService_ReturnsSnapshotAndUsesContractRoute()
    {
        var id = Guid.NewGuid();
        var handler = new StubHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.AbsolutePath.Should().Be($"/internal/v1/catalog/services/{id}");
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, $$"""{"id":"{{id}}","description":"Oil change","unitPrice":150.50,"isActive":true}"""));
        });
        using var client = CreateClient(handler);
        var acl = new CatalogInventoryAcl(client);

        var result = await acl.GetCatalogedServiceSnapshotAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new CatalogServiceSnapshot(id, "Oil change", 150.50m, true));
    }

    [Fact]
    public async Task GetInventoryItemSnapshotAsync_ActivePart_ReturnsSnapshotAndUsesContractRoute()
    {
        var id = Guid.NewGuid();
        var handler = new StubHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.AbsolutePath.Should().Be($"/internal/v1/catalog/parts/{id}");
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, $$"""{"id":"{{id}}","description":"Filter","unitPrice":35.25,"isActive":true,"quantityInStock":8}"""));
        });
        using var client = CreateClient(handler);
        var acl = new CatalogInventoryAcl(client);

        var result = await acl.GetInventoryItemSnapshotAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new InventoryItemSnapshot(id, "Filter", 35.25m, true, 8));
    }

    [Theory]
    [InlineData(true, HttpStatusCode.NotFound)]
    [InlineData(false, HttpStatusCode.NotFound)]
    [InlineData(true, HttpStatusCode.OK)]
    [InlineData(false, HttpStatusCode.OK)]
    public async Task GetSnapshotAsync_MissingOrInactiveItem_ReturnsFailure(bool service, HttpStatusCode status)
    {
        var id = Guid.NewGuid();
        using var client = CreateClient(new StubHandler((_, _) => Task.FromResult(JsonResponse(status,
            $$"""{"id":"{{id}}","description":"Inactive","unitPrice":1,"isActive":false,"quantityInStock":0}"""))));
        var acl = new CatalogInventoryAcl(client);

        var failed = service
            ? (await acl.GetCatalogedServiceSnapshotAsync(id)).IsFailure
            : (await acl.GetInventoryItemSnapshotAsync(id)).IsFailure;

        failed.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetSnapshotAsync_ServerFailure_PropagatesHttpError(bool service)
    {
        using var client = CreateClient(new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));
        var acl = new CatalogInventoryAcl(client);

        Func<Task> act = async () =>
        {
            if (service)
                await acl.GetCatalogedServiceSnapshotAsync(Guid.NewGuid());
            else
                await acl.GetInventoryItemSnapshotAsync(Guid.NewGuid());
        };

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetSnapshotAsync_Cancelled_PropagatesCancellation(bool service)
    {
        using var source = new CancellationTokenSource();
        using var client = CreateClient(new StubHandler(async (_, token) =>
        {
            source.Cancel();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var acl = new CatalogInventoryAcl(client);

        Func<Task> act = async () =>
        {
            if (service)
                await acl.GetCatalogedServiceSnapshotAsync(Guid.NewGuid(), source.Token);
            else
                await acl.GetInventoryItemSnapshotAsync(Guid.NewGuid(), source.Token);
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) => new(handler)
    {
        BaseAddress = new Uri("http://execution-service:8080")
    };

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
