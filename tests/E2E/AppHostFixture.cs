using Microsoft.Extensions.DependencyInjection;

namespace CatCar.E2E.Tests;

public class AppHostFixture : IAsyncLifetime
{
    public DistributedApplication Application { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CatCar_AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(httpClientBuilder =>
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            }));
        Application = await appHost.BuildAsync();
        await Application.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Application.DisposeAsync();
    }
}

[CollectionDefinition("AppHost")]
public class AppHostCollectionFixture : ICollectionFixture<AppHostFixture>
{
}
