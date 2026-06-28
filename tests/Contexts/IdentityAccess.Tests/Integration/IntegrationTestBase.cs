namespace CatCar.Contexts.IdentityAccess.Tests.Integration;

using CatCar.Contexts.IdentityAccess.Infrastructure;
using CatCar.Contexts.IdentityAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

/// <summary>
/// Base class for integration tests using Testcontainers PostgreSQL.
/// Provides a real PostgreSQL instance for testing EF Core behavior.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private DbContextOptions<IdentityAccessDbContext>? _options;

    protected IntegrationTestBase()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .Build();
    }

    /// <summary>
    /// Gets the database connection string for the PostgreSQL container.
    /// </summary>
    protected string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Gets the DbContext options configured for the test database.
    /// </summary>
    protected DbContextOptions<IdentityAccessDbContext> DbContextOptions
    {
        get
        {
            if (_options == null)
            {
                throw new InvalidOperationException("Database not initialized. Call InitializeAsync first.");
            }

            return _options;
        }
    }

    /// <summary>
    /// Creates a new DbContext instance connected to the test database.
    /// </summary>
    protected IdentityAccessDbContext CreateDbContext()
    {
        return new IdentityAccessDbContext(DbContextOptions);
    }

    /// <summary>
    /// Initializes the PostgreSQL container and creates the database schema.
    /// AC-024
    /// </summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        _options = new DbContextOptionsBuilder<IdentityAccessDbContext>()
            .UseNpgsql(ConnectionString)
            .AddInterceptors(new AuditInterceptor())
            .Options;

        using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Cleans up all data from the test database by truncating all tables.
    /// Targets the 'identity_access' schema.
    /// </summary>
    protected async Task CleanupDatabaseAsync()
    {
        if (_options == null) return;

        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(@"
            DO $$ DECLARE
                r RECORD;
            BEGIN
                FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'identity_access') LOOP
                    EXECUTE 'TRUNCATE TABLE identity_access.'""' || r.tablename || '""' CASCADE';
                END LOOP;
            END $$;
        ");
    }

    /// <summary>
    /// Disposes of the PostgreSQL container.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _container.StopAsync().ConfigureAwait(false);
        await _container.DisposeAsync().ConfigureAwait(false);
    }
}
