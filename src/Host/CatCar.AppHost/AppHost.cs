using System.Security.Cryptography;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Azure.Storage;
using Aspire.Hosting.Kubernetes;
using CatCar.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var isPublishMode = builder.ExecutionContext.IsPublishMode;

IResourceBuilder<IResourceWithConnectionString> catcarDb;
IResourceBuilder<ParameterResource> jwtSecretParam;
IResourceBuilder<ParameterResource> customerJwtSigningKeyParam;
IResourceBuilder<AzureStorageResource> authStorage;
IResourceBuilder<AzureApplicationInsightsResource>? appInsights = null;
IResourceBuilder<KubernetesEnvironmentResource>? aksWorkloads = null;
IResourceBuilder<AzureContainerAppEnvironmentResource>? authEnvironment = null;

if (!isPublishMode)
{
    var postgres = builder.AddPostgres("postgres")
        .WithDataVolume();
    catcarDb = postgres.AddDatabase("catcar");

    var devJwtSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var devCustomerSigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    jwtSecretParam = builder.AddParameter("jwt-secret", devJwtSecret, secret: true);
    customerJwtSigningKeyParam = builder.AddParameter("customer-jwt-signing-key", devCustomerSigningKey, secret: true);

    authStorage = builder.AddAzureStorage("auth-storage")
        .RunAsEmulator();
}
else
{
    catcarDb = builder.AddConnectionString("catcar");

    jwtSecretParam = builder.AddParameter("jwt-secret", secret: true);
    customerJwtSigningKeyParam = builder.AddParameter("customer-jwt-signing-key", secret: true);

    var foundationResourceGroup = builder.AddParameter("foundation-resource-group");
    var acrName = builder.AddParameter("acr-name");
    var logAnalyticsWorkspaceName = builder.AddParameter("log-analytics-workspace-name");
    var appInsightsName = builder.AddParameter("application-insights-name");

    var acr = builder.AddAzureContainerRegistry("cr")
        .PublishAsExisting(acrName, foundationResourceGroup);

    var logAnalytics = builder.AddAzureLogAnalyticsWorkspace("log")
        .PublishAsExisting(logAnalyticsWorkspaceName, foundationResourceGroup);

    appInsights = builder.AddAzureApplicationInsights("insights")
        .WithLogAnalyticsWorkspace(logAnalytics)
        .PublishAsExisting(appInsightsName, foundationResourceGroup);

    authStorage = builder.AddAzureStorage("auth-storage");

    aksWorkloads = builder.AddKubernetesEnvironment("aks-workloads")
        .WithAzureContainerRegistry(acr)
        .WithHelm(helm => helm
            .WithChartName("catcar")
            .WithReleaseName("catcar")
            .WithNamespace("catcar"));

    authEnvironment = builder.AddAzureContainerAppEnvironment("auth-environment")
        .WithAzureContainerRegistry(acr)
        .WithAzureLogAnalyticsWorkspace(logAnalytics);
}

var api = builder.AddProject<Projects.CatCar_Api>("api")
    .WithHttpEndpoint(targetPort: 8080, name: "http")
    .WithExternalHttpEndpoints()
    .WithReference(catcarDb)
    .WithEnvironment("Jwt__Secret", jwtSecretParam);

if (!isPublishMode)
{
    if (catcarDb is IResourceBuilder<PostgresDatabaseResource> postgresDb)
    {
        api.WaitFor(postgresDb);
    }
}
else
{
    if (appInsights is not null)
    {
        api.WithReference(appInsights);
    }

    if (aksWorkloads is not null)
    {
        api.WithComputeEnvironment(aksWorkloads);
    }

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
}

var authFunction = builder.AddAzureFunctionsProject<Projects.CatCar_AuthFunction>("auth-function")
    .WithHostStorage(authStorage)
    .WithExternalHttpEndpoints()
    .WithEnvironment("CustomerJwt__SigningKey", customerJwtSigningKeyParam)
    .WithEnvironment("CustomerJwt__Issuer", "CatCar")
    .WithEnvironment("CustomerJwt__Audience", "CatCar.Customer")
    .WithEnvironment("CustomerJwt__ExpirationMinutes", "60");

if (!isPublishMode)
{
    authFunction.WaitFor(authStorage);
}
else
{
    if (appInsights is not null)
    {
        authFunction.WithReference(appInsights);
    }

    if (authEnvironment is not null)
    {
        authFunction.WithComputeEnvironment(authEnvironment);
    }
}

builder.Build().Run();
