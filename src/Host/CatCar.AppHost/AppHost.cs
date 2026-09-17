using System.Security.Cryptography;
using CatCar.AppHost;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var isRunMode = builder.ExecutionContext.IsRunMode;
var isLocalKubernetesPublish = builder.ExecutionContext.IsPublishMode &&
                               builder.Environment.IsEnvironment("LocalKubernetes");

if (isRunMode)
{
    var postgres = builder.AddPostgres("postgres")
        .WithDataVolume();
    var catcarDb = postgres.AddDatabase("catcar");

    var devJwtSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var devCustomerSigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    var jwtSecretParam = builder.AddParameter("jwt-secret", devJwtSecret, secret: true);
    var customerJwtSigningKeyParam = builder.AddParameter("customer-jwt-signing-key", devCustomerSigningKey, secret: true);

    var authStorage = builder.AddAzureStorage("auth-storage")
        .RunAsEmulator();

    builder.AddProject<Projects.CatCar_Api>("api")
        .WithHttpsEndpoint(port: 5002, name: "https")
        .WithHttpEndpoint(port: 5000, name: "http")
        .WithExternalHttpEndpoints()
        .WithReference(catcarDb)
        .WithEnvironment("Jwt__Secret", jwtSecretParam)
        .WithEnvironment("CustomerJwt__SigningKey", customerJwtSigningKeyParam)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
        .WaitFor(catcarDb);

    builder.AddAzureFunctionsProject<Projects.CatCar_AuthFunction>("auth-function")
        .WithHostStorage(authStorage)
        .WithHttpEndpoint(port: 7071, name: "http")
        .WithExternalHttpEndpoints()
        .WithReference(catcarDb)
        .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated")
        .WithEnvironment("FUNCTIONS_EXTENSION_VERSION", "~4")
        .WithEnvironment("AZURE_FUNCTIONS_ENVIRONMENT", "Development")
        .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
        .WithEnvironment("CustomerJwt__SigningKey", customerJwtSigningKeyParam)
        .WithEnvironment("CustomerJwt__Issuer", "CatCar")
        .WithEnvironment("CustomerJwt__Audience", "CatCar.Api")
        .WithEnvironment("CustomerJwt__ExpirationMinutes", "60")
        .WithEnvironment("CustomerJwt__CorsOrigin", "http://localhost:7071")
        .WaitFor(catcarDb)
        .WaitFor(authStorage);
}
else if (isLocalKubernetesPublish)
{
#pragma warning disable ASPIRECOMPUTE003
    var localRegistry = builder.AddContainerRegistry("local-registry", "localhost:5001");
    var localKubernetes = builder.AddKubernetesEnvironment("local-kubernetes")
        .WithContainerRegistry(localRegistry)
        .WithHelm(helm => helm
            .WithChartName("catcar-local")
            .WithReleaseName("catcar-local")
            .WithNamespace("catcar-local"));

    var postgres = builder.AddPostgres("postgres")
        .WithComputeEnvironment(localKubernetes);
    var catcarDb = postgres.AddDatabase("catcar");

    var devJwtSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var jwtSecretParam = builder.AddParameter("jwt-secret", devJwtSecret, secret: true);

    var api = builder.AddProject<Projects.CatCar_Api>("api")
        .WithHttpEndpoint(targetPort: 8080, name: "http")
        .WithExternalHttpEndpoints()
        .WithReference(catcarDb)
        .WithEnvironment("Jwt__Secret", jwtSecretParam)
        .WithComputeEnvironment(localKubernetes)
        .WithContainerRegistry(localRegistry)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("DOTNET_ENVIRONMENT", "Development");

    api.ConfigureCatCarKubernetesWorkload(useKindDefaults: true);
#pragma warning restore ASPIRECOMPUTE003
}
else
{
    var catcarDb = builder.AddConnectionString("catcar");
    var authDb = builder.AddConnectionString("catcar-auth");

    var jwtSecretParam = builder.AddParameter("jwt-secret", secret: true);
    var customerJwtSigningKeyParam = builder.AddParameter("customer-jwt-signing-key", secret: true);

    var foundationResourceGroup = builder.AddParameter("foundation-resource-group");
    var acrName = builder.AddParameter("acr-name");
    var logAnalyticsWorkspaceName = builder.AddParameter("log-analytics-workspace-name");
    var appInsightsName = builder.AddParameter("application-insights-name");

    var acr = builder.AddAzureContainerRegistry("cr")
        .PublishAsExisting(acrName, foundationResourceGroup);

    var logAnalytics = builder.AddAzureLogAnalyticsWorkspace("log")
        .PublishAsExisting(logAnalyticsWorkspaceName, foundationResourceGroup);

    var appInsights = builder.AddAzureApplicationInsights("insights")
        .WithLogAnalyticsWorkspace(logAnalytics)
        .PublishAsExisting(appInsightsName, foundationResourceGroup);

    var authStorage = builder.AddAzureStorage("auth-storage");

    var aksWorkloads = builder.AddKubernetesEnvironment("aks-workloads")
        .WithAzureContainerRegistry(acr)
        .WithHelm(helm => helm
            .WithChartName("catcar")
            .WithReleaseName("catcar")
            .WithNamespace("catcar"));

    var authEnvironment = builder.AddAzureContainerAppEnvironment("auth-environment")
        .WithAzureContainerRegistry(acr)
        .WithAzureLogAnalyticsWorkspace(logAnalytics);

    var api = builder.AddProject<Projects.CatCar_Api>("api")
        .WithHttpEndpoint(targetPort: 8080, name: "http")
        .WithReference(catcarDb)
        .WithEnvironment("Jwt__Secret", jwtSecretParam)
        .WithEnvironment("CustomerJwt__SigningKey", customerJwtSigningKeyParam)
        .WithReference(appInsights)
        .WithComputeEnvironment(aksWorkloads);

    var apiImageName = builder.Configuration["Parameters:api-image-name"]
        ?? builder.Configuration["Parameters:api_image_name"];
    if (!string.IsNullOrWhiteSpace(apiImageName))
    {
        api.WithRemoteImageName(apiImageName);
    }

    var apiImageTag = builder.Configuration["Parameters:api-image-tag"]
        ?? builder.Configuration["Parameters:api_image_tag"];
    if (!string.IsNullOrWhiteSpace(apiImageTag))
    {
        api.WithRemoteImageTag(apiImageTag);
    }

    api.ConfigureCatCarKubernetesWorkload();

    builder.AddAzureFunctionsProject<Projects.CatCar_AuthFunction>("auth-function")
        .WithHostStorage(authStorage)
        .WithReference(authDb)
        .WithEnvironment("CustomerJwt__SigningKey", customerJwtSigningKeyParam)
        .WithEnvironment("CustomerJwt__Issuer", "CatCar")
        .WithEnvironment("CustomerJwt__Audience", "CatCar.Api")
        .WithEnvironment("CustomerJwt__ExpirationMinutes", "60")
        .WithReference(appInsights)
        .WithComputeEnvironment(authEnvironment);
}

builder.Build().Run();
