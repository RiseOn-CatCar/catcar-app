namespace CatCar.Contexts.CatalogInventory.Tests.Integration;

using CatCar.Contexts.CatalogInventory.Infrastructure;
using CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;
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
    private DbContextOptions<CatalogInventoryDbContext>? _options;

    protected IntegrationTestBase()
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
    }

    /// <summary>
    /// Gets the database connection string for the PostgreSQL container.
    /// </summary>
    protected string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Gets the DbContext options configured for the test database.
    /// </summary>
    protected DbContextOptions<CatalogInventoryDbContext> DbContextOptions
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
    protected CatalogInventoryDbContext CreateDbContext()
    {
        return new CatalogInventoryDbContext(DbContextOptions);
    }

    /// <summary>
    /// Initializes the PostgreSQL container and creates the database schema.
    /// AC-024
    /// </summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        _options = new DbContextOptionsBuilder<CatalogInventoryDbContext>()
            .UseNpgsql(ConnectionString)
            .AddInterceptors(new AuditInterceptor())
            .Options;

        using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Cleans up all data from the test database by truncating all tables.
    /// Targets the 'catalog_inventory' schema.
    /// </summary>
    protected async Task CleanupDatabaseAsync()
    {
        if (_options == null) return;

        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(@"
            DO $$ DECLARE
                r RECORD;
            BEGIN
                FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'catalog_inventory') LOOP
                    EXECUTE 'TRUNCATE TABLE catalog_inventory.'""' || r.tablename || '""' CASCADE';
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
