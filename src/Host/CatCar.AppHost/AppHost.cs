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

    var authFunctionProjectPath = Path.GetFullPath(
        "../../../catcar-auth-function/src/CatCar.AuthFunction/CatCar.AuthFunction.csproj",
        builder.Environment.ContentRootPath);
    if (!File.Exists(authFunctionProjectPath))
    {
        authFunctionProjectPath = Path.GetFullPath(
            "../../../../catcar-auth-function/src/CatCar.AuthFunction/CatCar.AuthFunction.csproj",
            builder.Environment.ContentRootPath);
    }

    var authStorage = builder.AddAzureStorage("auth-storage")
        .RunAsEmulator();

    builder.AddDotnetProject("api", "../../Api/CatCar.Api.csproj")
        .WithHttpsEndpoint(port: 5002, name: "https")
        .WithHttpEndpoint(port: 5000, name: "http")
        .WithExternalHttpEndpoints()
        .WithReference(catcarDb)
        .WithEnvironment("Jwt__Secret", jwtSecretParam)
        .WithEnvironment("CustomerJwt__SigningKey", customerJwtSigningKeyParam)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
        .WaitFor(catcarDb);

    builder.AddAzureFunctionsProject("auth-function", authFunctionProjectPath)
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

    var apiImage = builder.Configuration["Parameters:local-api-image"]
        ?? builder.Configuration["Parameters:local_api_image"]
        ?? throw new InvalidOperationException("The local API image digest is required.");
    var authImage = builder.Configuration["Parameters:local-auth-function-image"]
        ?? builder.Configuration["Parameters:local_auth_function_image"]
        ?? throw new InvalidOperationException("The local authentication function image digest is required.");

    var postgresData = localKubernetes.AddPersistentVolume("postgres-data")
        .WithCapacity("1Gi")
        .WithVolumeAnnotation("helm.sh/resource-policy", "keep");
    var postgres = builder.AddContainer("postgres", "postgres", "17-alpine")
        .WithEndpoint(targetPort: 5432, name: "tcp")
        .WithVolume("postgres-data", "/var/lib/postgresql/data")
        .WithPersistentVolume(postgresData)
        .WithEnvironment("POSTGRES_USER", "postgres")
        .WithEnvironment("POSTGRES_DB", "catcar")
        .WithComputeEnvironment(localKubernetes);
    postgres.ConfigureLocalPostgres();

    var mailpit = builder.AddContainer("mailpit", "axllent/mailpit", "v1.34.0")
        .WithHttpEndpoint(targetPort: 8025, name: "http")
        .WithComputeEnvironment(localKubernetes);
    mailpit.ConfigureLocalService("mailpit", 8025);

    const string storageKey = "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=";
    var azurite = builder.AddContainer("azurite", "mcr.microsoft.com/azure-storage/azurite", "3.37.0")
        .WithArgs("azurite", "-l", "/data", "--blobHost", "0.0.0.0", "--queueHost", "0.0.0.0", "--tableHost", "0.0.0.0")
        .WithEnvironment("AZURITE_ACCOUNTS", $"devstoreaccount1:{storageKey}")
        .WithEndpoint(targetPort: 10000, name: "blob")
        .WithEndpoint(targetPort: 10001, name: "queue")
        .WithEndpoint(targetPort: 10002, name: "table")
        .WithComputeEnvironment(localKubernetes);
    azurite.ConfigureLocalAzurite();

    var api = builder.AddContainer("api", "catcar-api")
        .WithHttpEndpoint(targetPort: 8080, name: "http")
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "LocalKubernetes")
        .WithEnvironment("DOTNET_ENVIRONMENT", "LocalKubernetes")
        .WithEnvironment("Communication__MailpitBaseUrl", "http://mailpit:8025")
        .WithEnvironment("Communication__SenderAddress", "noreply@catcar.local")
        .WithEnvironment("Communication__ExternalBaseUrl", "http://localhost:8080")
        .WithComputeEnvironment(localKubernetes);
    api.ConfigureCatCarKubernetesWorkload(useKindDefaults: true, image: apiImage);

    var auth = builder.AddContainer("auth-function", "catcar-auth-function")
        .WithHttpEndpoint(targetPort: 80, name: "http")
        .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated")
        .WithEnvironment("AzureWebJobsStorage", $"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey={storageKey};BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;")
        .WithEnvironment("FUNCTIONS_EXTENSION_VERSION", "~4")
        .WithEnvironment("CustomerJwt__Issuer", "CatCar")
        .WithEnvironment("CustomerJwt__Audience", "CatCar.Api")
        .WithEnvironment("CustomerJwt__ExpirationMinutes", "60")
        .WithEnvironment("CustomerJwt__CorsOrigin", "http://localhost:7071")
        .WithComputeEnvironment(localKubernetes);
    auth.ConfigureLocalService("catcar-auth-function", 80, authImage);
#pragma warning restore ASPIRECOMPUTE003
}
else
{
    var catcarDb = builder.AddConnectionString("catcar");

    var jwtSecretParam = builder.AddParameter("jwt-secret", secret: true);
    var customerJwtSigningKeyParam = builder.AddParameter("customer-jwt-signing-key", secret: true);
    var communicationConnection = builder.AddParameter("communication-connection-string", secret: true);
    var communicationSender = builder.AddParameter("communication-sender-address");
    var communicationBaseUrl = builder.AddParameter("communication-external-base-url");

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

    var aksWorkloads = builder.AddKubernetesEnvironment("aks-workloads")
        .WithAzureContainerRegistry(acr)
        .WithHelm(helm => helm
            .WithChartName("catcar")
            .WithReleaseName("catcar")
            .WithNamespace("catcar"));

    var apiImageName = builder.Configuration["Parameters:api-image-name"]
        ?? builder.Configuration["Parameters:api_image_name"]
        ?? throw new InvalidOperationException("The cloud API image name is required.");
    var apiImageDigest = builder.Configuration["Parameters:api-image-digest"]
        ?? builder.Configuration["Parameters:api_image_digest"]
        ?? throw new InvalidOperationException("The cloud API image digest is required.");

    var api = builder.AddContainer("api", apiImageName)
        .WithImageSHA256(apiImageDigest.StartsWith("sha256:", StringComparison.Ordinal) ? apiImageDigest[7..] : apiImageDigest)
        .WithHttpEndpoint(targetPort: 8080, name: "http")
        .WithReference(catcarDb)
        .WithEnvironment("Jwt__Secret", jwtSecretParam)
        .WithEnvironment("CustomerJwt__SigningKey", customerJwtSigningKeyParam)
        .WithEnvironment("Communication__AzureCommunicationServicesConnectionString", communicationConnection)
        .WithEnvironment("Communication__SenderAddress", communicationSender)
        .WithEnvironment("Communication__ExternalBaseUrl", communicationBaseUrl)
        .WithReference(appInsights)
        .WithComputeEnvironment(aksWorkloads);

    api.ConfigureCatCarKubernetesWorkload(
        apiPrivateIp: builder.Configuration["Parameters:api_private_ip"],
        image: $"{apiImageName}@{apiImageDigest}");
}

builder.Build().Run();
