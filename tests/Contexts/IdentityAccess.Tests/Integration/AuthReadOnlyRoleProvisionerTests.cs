namespace CatCar.Contexts.IdentityAccess.Tests.Integration;

using CatCar.Api;
using FluentAssertions;
using Npgsql;
using Xunit;

public sealed class AuthReadOnlyRoleProvisionerTests : IntegrationTestBase
{
    [Fact]
    public async Task ProvisionAsync_ExistingExcessGrants_RestrictsAndAuthenticates()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using (var setup = new NpgsqlCommand("""
            CREATE SCHEMA service_operations;
            CREATE TABLE service_operations.customers (id uuid PRIMARY KEY);
            CREATE SCHEMA another_context;
            CREATE TABLE another_context.private_data (id integer);
            CREATE TABLE service_operations.private_data (id integer);
            CREATE ROLE readonly_test LOGIN PASSWORD 'oldpassword' CREATEDB;
            GRANT ALL ON SCHEMA service_operations TO readonly_test;
            GRANT USAGE ON SCHEMA another_context TO readonly_test;
            GRANT SELECT ON another_context.private_data TO readonly_test;
            GRANT ALL ON ALL TABLES IN SCHEMA service_operations TO readonly_test;
            """, connection))
            await setup.ExecuteNonQueryAsync();

        await AuthReadOnlyRoleProvisioner.ProvisionAsync(ConnectionString, "readonly_test", "newpassword", CancellationToken.None);
        await AuthReadOnlyRoleProvisioner.ProvisionAsync(ConnectionString, "readonly_test", "newpassword", CancellationToken.None);
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString) { Username = "readonly_test", Password = "newpassword", Pooling = false };
        await using var restricted = new NpgsqlConnection(builder.ConnectionString);
        await restricted.OpenAsync();
        await using var select = new NpgsqlCommand("SELECT count(*) FROM service_operations.customers", restricted);
        (await select.ExecuteScalarAsync()).Should().Be(0L);
        await using var forbidden = new NpgsqlCommand("SELECT * FROM another_context.private_data", restricted);
        var action = () => forbidden.ExecuteNonQueryAsync();
        var error = await action.Should().ThrowAsync<PostgresException>();
        error.Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }
}
