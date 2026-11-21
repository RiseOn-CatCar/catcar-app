namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using CatCar.Contexts.IdentityAccess.Infrastructure.Seed;
using CatCar.Contexts.ServiceOperations.Integrations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Wolverine;
using Xunit;

/// <summary>
/// Group shared by every in-process HTTP integration test: one PostgreSQL container and one API host.
/// </summary>
[CollectionDefinition(ApiIntegrationGroup.Name)]
public sealed class ApiIntegrationGroup : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}

/// <summary>
/// Boots <c>Program</c> in-process against a PostgreSQL Testcontainer. External transports (RabbitMQ) are stubbed,
/// the execution-service catalog is replaced by <see cref="FakeExecutionCatalog"/>, and JWTs are signed with the
/// same test secret the API validates.
/// Usage: <c>[Collection(ApiIntegrationGroup.Name)] public sealed class MyTests(ApiFixture api)</c>, then use
/// <see cref="Client"/> (anonymous), <see cref="AdminClient"/>, <see cref="ReceptionistClient"/>,
/// <see cref="TechnicianClient"/> or <see cref="CreateClient(string)"/> for another role.
/// </summary>
#pragma warning disable CA1001 // xUnit disposes the fixture through IAsyncLifetime.DisposeAsync.
public sealed class ApiFixture : IAsyncLifetime
{
    public const string JwtSecret = "catcar-integration-test-signing-key-0123456789-abcdef";
    public const string InternalApiKey = "integration-test-internal-api-key";
    public const string JwtIssuer = "CatCar";
    public const string JwtAudience = "CatCar.AdminApi";
    public const string ServiceOperationsPrefix = "/api/v1/service-operations";
    public const string IdentityAccessPrefix = "/api/v1/identity-access";

    // Seeded by Program in the Development environment.
    public static string AdminEmail => AdministrativeUserSeeder.DefaultAdminEmail;

    public static string AdminPassword => AdministrativeUserSeeder.DefaultAdminPassword;

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly Dictionary<string, string?> _previousEnvironment = [];
    private ApiFactory? _factory;

    /// <summary>Gets the catalog stub answering the order service's execution-service calls.</summary>
    public FakeExecutionCatalog Catalog { get; } = new();

    /// <summary>Gets the API's root service provider (scoped services need a scope).</summary>
    public IServiceProvider Services => Factory.Services;

    /// <summary>Gets an anonymous client.</summary>
    public HttpClient Client { get; private set; } = null!;

    /// <summary>Gets a client authenticated as an Administrador.</summary>
    public HttpClient AdminClient { get; private set; } = null!;

    /// <summary>Gets a client authenticated as a Recepcionista.</summary>
    public HttpClient ReceptionistClient { get; private set; } = null!;

    /// <summary>Gets a client authenticated as a Tecnico.</summary>
    public HttpClient TechnicianClient { get; private set; } = null!;

    private ApiFactory Factory => _factory ?? throw new InvalidOperationException("The API fixture is not initialized.");

    public async Task InitializeAsync()
    {
        await _database.StartAsync().ConfigureAwait(false);

        // Program reads configuration while building the host, before WebApplicationFactory callbacks run,
        // so the settings are provided as process environment variables for the lifetime of the fixture.
        SetEnvironment("ConnectionStrings__catcar", _database.GetConnectionString());
        SetEnvironment("ConnectionStrings__messaging", "amqp://guest:guest@localhost:5672");
        SetEnvironment("Jwt__Secret", JwtSecret);
        SetEnvironment("Jwt__Issuer", JwtIssuer);
        SetEnvironment("Jwt__Audience", JwtAudience);
        SetEnvironment("CustomerJwt__SigningKey", JwtSecret);
        SetEnvironment("InternalApi__Key", InternalApiKey);
        SetEnvironment("Services__Execution", "http://execution-service:8080");

        _factory = new ApiFactory(Catalog);
        Client = _factory.CreateClient();
        AdminClient = CreateClient(nameof(AdministrativeRole.Administrador));
        ReceptionistClient = CreateClient(nameof(AdministrativeRole.Recepcionista));
        TechnicianClient = CreateClient(nameof(AdministrativeRole.Tecnico));

        // Forces host startup (migrations and bootstrap administrator) before the first test.
        using var probe = await Client.GetAsync("/health/live").ConfigureAwait(false);
        probe.EnsureSuccessStatusCode();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        AdminClient?.Dispose();
        ReceptionistClient?.Dispose();
        TechnicianClient?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync().ConfigureAwait(false);

        foreach (var (key, value) in _previousEnvironment)
            Environment.SetEnvironmentVariable(key, value);

        await _database.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>Creates a client carrying a valid JWT for <paramref name="role"/> (Administrador, Recepcionista or Tecnico).</summary>
    public HttpClient CreateClient(string role)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role));
        return client;
    }

    /// <summary>Signs a JWT with the test secret using the claims issued by the real token generator.</summary>
    public static string CreateToken(string role, string email = "integration@catcar.test", DateTime? expiresAtUtc = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.CreateVersion7().ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(ClaimTypes.Name, "Integration Test"),
            new(ClaimTypes.Role, role),
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(JwtIssuer, JwtAudience, claims, expires: expiresAtUtc ?? DateTime.UtcNow.AddMinutes(30), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Logs in through the real endpoint and returns the status and (when successful) the access token.</summary>
    public async Task<(HttpStatusCode Status, string? AccessToken)> LoginAsync(string email, string password)
    {
        using var response = await Client.PostAsJsonAsync($"{IdentityAccessPrefix}/auth/login", new { email, password }).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return (response.StatusCode, null);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>().ConfigureAwait(false);
        return (response.StatusCode, body?.AccessToken);
    }

    /// <summary>Generates a valid, unique CPF (digits only).</summary>
    public static string NewCpf()
    {
        var digits = new int[11];
        do
        {
            for (var i = 0; i < 9; i++)
                digits[i] = RandomNumberGenerator.GetInt32(10);
        }
        while (digits.Take(9).Distinct().Count() == 1);

        digits[9] = CheckDigit(digits, 9);
        digits[10] = CheckDigit(digits, 10);
        return new string(digits.Select(d => (char)('0' + d)).ToArray());

        static int CheckDigit(int[] value, int length)
        {
            var sum = 0;
            for (var i = 0; i < length; i++)
                sum += value[i] * (length + 1 - i);
            var remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }
    }

    /// <summary>Generates a unique Mercosul license plate.</summary>
    public static string NewPlate()
    {
        static char Letter() => (char)('A' + RandomNumberGenerator.GetInt32(26));
        static char Digit() => (char)('0' + RandomNumberGenerator.GetInt32(10));
        return new string([Letter(), Letter(), Letter(), Digit(), Letter(), Digit(), Digit()]);
    }

    /// <summary>Registers a customer through the API and returns its id.</summary>
    public async Task<Guid> CreateCustomerAsync(string? email = "customer@example.com", string name = "Integration Customer")
    {
        using var response = await AdminClient.PostAsJsonAsync(
            $"{ServiceOperationsPrefix}/customers",
            new { document = NewCpf(), name, phone = "85999990000", email }).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<IdResponse>().ConfigureAwait(false);
        return body!.Id;
    }

    /// <summary>Registers a vehicle for <paramref name="customerId"/> through the API and returns its id.</summary>
    public async Task<Guid> CreateVehicleAsync(Guid customerId)
    {
        using var response = await AdminClient.PostAsJsonAsync(
            $"{ServiceOperationsPrefix}/vehicles",
            new { customerId, plate = NewPlate(), brand = "Fiat", model = "Uno", manufactureYear = 2015 }).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<IdResponse>().ConfigureAwait(false);
        return body!.Id;
    }

    private void SetEnvironment(string key, string value)
    {
        _previousEnvironment[key] = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, value);
    }

    private sealed record IdResponse(Guid Id);

    private sealed record LoginResponse(string AccessToken);

    private sealed class ApiFactory(FakeExecutionCatalog catalog) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.ConfigureWolverine(options =>
                {
                    options.StubAllExternalTransports();
                    options.UseRuntimeCompilation();
                });
                services.AddHttpClient<ICatalogInventoryAcl, CatalogInventoryAcl>()
                    .ConfigurePrimaryHttpMessageHandler(() => new FakeExecutionCatalogHandler(catalog));
            });
        }
    }
}
#pragma warning restore CA1001

/// <summary>
/// Catalog data served by the fake execution-service. Ids that are not registered answer 404.
/// </summary>
public sealed class FakeExecutionCatalog
{
    /// <summary>Active cataloged service "Oil change" priced at 150.00.</summary>
    public static readonly Guid ServiceId = Guid.Parse("0197a000-0000-7000-8000-000000000001");

    /// <summary>Cataloged service that exists but is inactive.</summary>
    public static readonly Guid InactiveServiceId = Guid.Parse("0197a000-0000-7000-8000-000000000002");

    /// <summary>Active part "Oil filter" priced at 40.00 with 100 units in stock.</summary>
    public static readonly Guid PartId = Guid.Parse("0197a000-0000-7000-8000-000000000011");

    /// <summary>Part that exists but is inactive.</summary>
    public static readonly Guid InactivePartId = Guid.Parse("0197a000-0000-7000-8000-000000000012");

    private readonly ConcurrentDictionary<Guid, CatalogServiceSnapshot> _services = new();
    private readonly ConcurrentDictionary<Guid, InventoryItemSnapshot> _parts = new();

    public FakeExecutionCatalog()
    {
        AddService(ServiceId, "Oil change", 150m);
        AddService(InactiveServiceId, "Retired service", 99m, isActive: false);
        AddPart(PartId, "Oil filter", 40m, quantityInStock: 100);
        AddPart(InactivePartId, "Retired part", 10m, isActive: false, quantityInStock: 5);
    }

    public void AddService(Guid id, string description, decimal unitPrice, bool isActive = true)
        => _services[id] = new CatalogServiceSnapshot(id, description, unitPrice, isActive);

    public void AddPart(Guid id, string description, decimal unitPrice, bool isActive = true, int quantityInStock = 100)
        => _parts[id] = new InventoryItemSnapshot(id, description, unitPrice, isActive, quantityInStock);

    internal object? Find(string kind, Guid id)
        => kind == "services"
            ? _services.GetValueOrDefault(id)
            : _parts.GetValueOrDefault(id);
}

#pragma warning disable CA2000 // The HttpClient pipeline owns and disposes the returned responses.
internal sealed class FakeExecutionCatalogHandler(FakeExecutionCatalog catalog) : HttpMessageHandler
{
    private const string Prefix = "/internal/v1/catalog/";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        if (request.Method == HttpMethod.Get && path.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var segments = path[Prefix.Length..].Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 2 && segments[0] is "services" or "parts" && Guid.TryParse(segments[1], out var id))
            {
                var snapshot = catalog.Find(segments[0], id);
                if (snapshot is not null)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(snapshot, snapshot.GetType()) });
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
#pragma warning restore CA2000
