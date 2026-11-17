namespace CatCar.Contexts.IdentityAccess.Tests.Integration;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Testcontainers.PostgreSql;
using Xunit;

public sealed class ProductionWolverineTests
{
    [Fact]
    public async Task LocalKubernetes_HandlersResolveScopedDependenciesAcrossContexts()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "CatCar.slnx")))
            root = root.Parent;
        root.Should().NotBeNull("the test runs from the app checkout");
        var publishDirectory = Path.Combine(Path.GetTempPath(), $"catcar-publish-{Guid.NewGuid():N}");
        Directory.CreateDirectory(publishDirectory);
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        using (var publish = Process.Start(new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            WorkingDirectory = root!.FullName,
            ArgumentList =
            {
                "publish", "src/Api/CatCar.Api.csproj", "-c", configuration, "--no-build", "--no-restore", "-o", publishDirectory,
            },
        })!)
        {
            await publish.WaitForExitAsync();
            publish.ExitCode.Should().Be(0);
        }
        await using var database = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await database.StartAsync();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var address = $"http://127.0.0.1:{port}";
        var output = new StringBuilder();

        Process StartApi(params string[] arguments)
        {
            var start = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = publishDirectory,
            };
            start.ArgumentList.Add(Path.Combine(publishDirectory, "CatCar.Api.dll"));
            foreach (var argument in arguments) start.ArgumentList.Add(argument);
            start.Environment["DOTNET_ENVIRONMENT"] = "LocalKubernetes";
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "LocalKubernetes";
            start.Environment["ASPNETCORE_URLS"] = address;
            start.Environment["ConnectionStrings__catcar"] = database.GetConnectionString();
            start.Environment["Jwt__Secret"] = "test-admin-signing-key-at-least-32-bytes";
            start.Environment["CustomerJwt__SigningKey"] = "test-customer-signing-key-at-least-32-bytes";
            start.Environment["BootstrapAdmin__Email"] = "admin@catcar.test";
            start.Environment["BootstrapAdmin__Password"] = "Catcar@Admin123";
            start.Environment["Communication__MailpitBaseUrl"] = "http://127.0.0.1:8025";
            start.Environment["Communication__SenderAddress"] = "noreply@catcar.test";
            start.Environment["Communication__ExternalBaseUrl"] = address;
            var process = Process.Start(start)!;
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            return process;
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        using (var migration = StartApi("--migrate", "--bootstrap-admin"))
        {
            try
            {
                await migration.WaitForExitAsync(timeout.Token);
                migration.ExitCode.Should().Be(0, output.ToString());
            }
            finally
            {
                if (!migration.HasExited) migration.Kill(entireProcessTree: true);
            }
        }

        using var api = StartApi();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            while (true)
            {
                if (api.HasExited) throw new InvalidOperationException($"API exited: {output}");
                try
                {
                    using var ready = await client.GetAsync("/health/ready", timeout.Token);
                    if (ready.IsSuccessStatusCode) break;
                }
                catch (HttpRequestException) when (!timeout.IsCancellationRequested) { }
                await Task.Delay(250, timeout.Token);
            }

            using var login = await client.PostAsJsonAsync("/api/v1/identity-access/auth/login",
                new { email = "admin@catcar.test", password = "Catcar@Admin123" }, timeout.Token);
            login.StatusCode.Should().Be(HttpStatusCode.OK, await login.Content.ReadAsStringAsync(timeout.Token));
            using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync(timeout.Token));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString());

            using var orders = await client.GetAsync("/api/v1/service-operations/work-orders", timeout.Token);
            orders.StatusCode.Should().Be(HttpStatusCode.OK, await orders.Content.ReadAsStringAsync(timeout.Token));
            using var approval = await client.GetAsync("/api/v1/communication/approval-links/nonexistent-token", timeout.Token);
            approval.StatusCode.Should().Be(HttpStatusCode.NotFound, await approval.Content.ReadAsStringAsync(timeout.Token));
            using var inventory = await client.PostAsJsonAsync($"/api/v1/catalog-inventory/inventory-items/{Guid.NewGuid()}/stock-movements",
                new { movementType = "Entrada", quantity = 1, reason = "test" }, timeout.Token);
            inventory.StatusCode.Should().Be(HttpStatusCode.BadRequest, await inventory.Content.ReadAsStringAsync(timeout.Token));
            using var inventoryError = JsonDocument.Parse(await inventory.Content.ReadAsStringAsync(timeout.Token));
            inventoryError.RootElement.GetProperty("detail").GetString().Should().Be("Peça/insumo não encontrada.");

            using var customer = await client.PostAsJsonAsync("/api/v1/service-operations/customers",
                new { document = "52998224725", name = "Test Customer", phone = "11999999999", email = "customer@catcar.test" }, timeout.Token);
            customer.StatusCode.Should().Be(HttpStatusCode.Created, await customer.Content.ReadAsStringAsync(timeout.Token));
            using var customerJson = JsonDocument.Parse(await customer.Content.ReadAsStringAsync(timeout.Token));
            var customerId = customerJson.RootElement.GetProperty("id").GetGuid();
            using var vehicle = await client.PostAsJsonAsync("/api/v1/service-operations/vehicles",
                new { customerId, plate = "CAT1234", brand = "CatCar", model = "Test", manufactureYear = 2020 }, timeout.Token);
            vehicle.StatusCode.Should().Be(HttpStatusCode.Created, await vehicle.Content.ReadAsStringAsync(timeout.Token));
            using var vehicleJson = JsonDocument.Parse(await vehicle.Content.ReadAsStringAsync(timeout.Token));
            var vehicleId = vehicleJson.RootElement.GetProperty("id").GetGuid();

            foreach (var isService in new[] { true, false })
            {
                var item = new[] { new { id = Guid.NewGuid(), quantity = 1 } };
                using var order = await client.PostAsJsonAsync("/api/v1/service-operations/work-orders",
                    new { customerId, vehicleId, initialDescription = "Test order", services = isService ? item : [], parts = isService ? [] : item }, timeout.Token);
                order.StatusCode.Should().Be(HttpStatusCode.BadRequest, await order.Content.ReadAsStringAsync(timeout.Token));
                using var orderError = JsonDocument.Parse(await order.Content.ReadAsStringAsync(timeout.Token));
                orderError.RootElement.GetProperty("detail").GetString().Should().Be(isService
                    ? "Serviço não encontrado no catálogo."
                    : "Peça/insumo não encontrado no estoque.");
            }
        }
        finally
        {
            if (!api.HasExited) api.Kill(entireProcessTree: true);
            await api.WaitForExitAsync(CancellationToken.None);
            Directory.Delete(publishDirectory, recursive: true);
        }
    }
}
