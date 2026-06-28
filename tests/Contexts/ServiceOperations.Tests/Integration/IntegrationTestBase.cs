namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using CatCar.Contexts.ServiceOperations.Infrastructure;
using CatCar.Contexts.ServiceOperations.Infrastructure.Persistence;
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
    private DbContextOptions<ServiceOperationsDbContext>? _options;

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
    protected DbContextOptions<ServiceOperationsDbContext> DbContextOptions
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
    protected ServiceOperationsDbContext CreateDbContext()
    {
        return new ServiceOperationsDbContext(DbContextOptions);
    }

    /// <summary>
    /// Initializes the PostgreSQL container and creates the database schema.
    /// AC-008, AC-012, AC-013, AC-014
    /// </summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        _options = new DbContextOptionsBuilder<ServiceOperationsDbContext>()
            .UseNpgsql(ConnectionString)
            .AddInterceptors(new AuditInterceptor())
            .Options;

        // Ensure database is created and schema is up to date
        using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Cleans up all data from the test database by truncating all tables.
    /// Called before each test to ensure a clean state.
    /// Targets the 'service_operations' schema, consistent with AC-008 schema-per-BC.
    /// </summary>
    protected async Task CleanupDatabaseAsync()
    {
        if (_options == null) return;

        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(@"
            DO $$ DECLARE
                r RECORD;
            BEGIN
                FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'service_operations') LOOP
                    EXECUTE 'TRUNCATE TABLE service_operations.""' || r.tablename || '""' CASCADE';
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
