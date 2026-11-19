namespace CatCar.Api;

using Npgsql;

public static class AuthReadOnlyRoleProvisioner
{
    public static async Task ProvisionAsync(string connectionString, string login, string password, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(login);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var connection = new NpgsqlConnection(connectionString);
        await using var connectionDisposal = connection.ConfigureAwait(false);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using var transactionDisposal = transaction.ConfigureAwait(false);
        var command = new NpgsqlCommand("""
            SELECT pg_advisory_xact_lock(hashtext('catcar.auth-readonly.provision'));
            SELECT set_config('catcar.readonly_login', @login, true), set_config('catcar.readonly_password', @password, true);
            DO $body$
            DECLARE
                role_name text := current_setting('catcar.readonly_login');
                role_password text := current_setting('catcar.readonly_password');
                item record;
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                    EXECUTE format('CREATE ROLE %I LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD %L', role_name, role_password);
                ELSE
                    EXECUTE format('ALTER ROLE %I LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD %L', role_name, role_password);
                END IF;
                FOR item IN SELECT parent.rolname FROM pg_auth_members m
                    JOIN pg_roles parent ON parent.oid = m.roleid
                    JOIN pg_roles member ON member.oid = m.member WHERE member.rolname = role_name
                LOOP
                    EXECUTE format('REVOKE %I FROM %I', item.rolname, role_name);
                END LOOP;
                EXECUTE format('REVOKE ALL PRIVILEGES ON DATABASE %I FROM %I', current_database(), role_name);
                FOR item IN SELECT nspname FROM pg_namespace
                    WHERE nspname NOT LIKE 'pg_%' AND nspname <> 'information_schema'
                LOOP
                    EXECUTE format('REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA %I FROM %I', item.nspname, role_name);
                    EXECUTE format('REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA %I FROM %I', item.nspname, role_name);
                    EXECUTE format('REVOKE ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA %I FROM %I', item.nspname, role_name);
                    EXECUTE format('REVOKE ALL PRIVILEGES ON SCHEMA %I FROM %I', item.nspname, role_name);
                END LOOP;
                EXECUTE format('GRANT CONNECT ON DATABASE %I TO %I', current_database(), role_name);
                EXECUTE format('GRANT USAGE ON SCHEMA service_operations TO %I', role_name);
                EXECUTE format('GRANT SELECT ON TABLE service_operations.customers TO %I', role_name);
            END $body$;
            """, connection, transaction);
        await using (command.ConfigureAwait(false))
        {
            command.Parameters.AddWithValue("login", login);
            command.Parameters.AddWithValue("password", password);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        // Check effective privileges, including grants inherited from PUBLIC, before commit.
        var verify = new NpgsqlCommand("""
            SELECT has_database_privilege(@login, current_database(), 'CONNECT')
                AND has_schema_privilege(@login, 'service_operations', 'USAGE')
                AND has_table_privilege(@login, 'service_operations.customers', 'SELECT')
                AND NOT has_database_privilege(@login, current_database(), 'CREATE')
                AND NOT EXISTS (SELECT 1 FROM pg_namespace n WHERE n.nspname NOT LIKE 'pg_%'
                    AND n.nspname <> 'information_schema' AND has_schema_privilege(@login, n.oid, 'CREATE'))
                AND NOT EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE n.nspname NOT LIKE 'pg_%' AND n.nspname <> 'information_schema'
                      AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
                      AND (has_table_privilege(@login, c.oid, 'INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER')
                        OR (NOT (n.nspname = 'service_operations' AND c.relname = 'customers')
                            AND has_table_privilege(@login, c.oid, 'SELECT'))));
            """, connection, transaction);
        await using (verify.ConfigureAwait(false))
        {
            verify.Parameters.AddWithValue("login", login);
            if (await verify.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not true)
                throw new InvalidOperationException("Readonly role has missing or excessive effective privileges.");
        }
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        var readonlyConnectionString = new NpgsqlConnectionStringBuilder(connectionString) { Username = login, Password = password, Pooling = false };
        var readonlyConnection = new NpgsqlConnection(readonlyConnectionString.ConnectionString);
        await using var readonlyConnectionDisposal = readonlyConnection.ConfigureAwait(false);
        await readonlyConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var read = new NpgsqlCommand("SELECT 1 FROM service_operations.customers LIMIT 1", readonlyConnection);
        await using var readDisposal = read.ConfigureAwait(false);
        await read.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        foreach (var sql in new[]
        {
            "INSERT INTO service_operations.customers DEFAULT VALUES",
            "UPDATE service_operations.customers SET id = id WHERE false",
            "DELETE FROM service_operations.customers WHERE false",
            "CREATE TABLE service_operations.catcar_readonly_privilege_probe (id integer)"
        })
        {
            var probeTransaction = await readonlyConnection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using var probeTransactionDisposal = probeTransaction.ConfigureAwait(false);
            try
            {
                var probe = new NpgsqlCommand(sql, readonlyConnection, probeTransaction);
                await using var probeDisposal = probe.ConfigureAwait(false);
                await probe.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException("Readonly role unexpectedly permitted a write or DDL operation.");
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InsufficientPrivilege)
            {
                // Expected: never commit a privilege probe.
            }
            finally
            {
                await probeTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
