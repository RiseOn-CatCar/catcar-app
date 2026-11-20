namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using CatCar.Contexts.ServiceOperations.Infrastructure;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Test-only context that reuses the production model and adds fixture entities.
/// The production <see cref="ServiceOperationsDbContext"/> must never map these tables.
/// </summary>
public sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : ServiceOperationsDbContext(options)
{
    public DbSet<TestAuditableEntity> TestAuditableEntities => Set<TestAuditableEntity>();

    public DbSet<TestConcurrencyEntity> TestConcurrencyEntities => Set<TestConcurrencyEntity>();

    public DbSet<TestSnakeCaseEntity> TestSnakeCaseEntities => Set<TestSnakeCaseEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureTestEntity<TestAuditableEntity>(modelBuilder);
        ConfigureTestEntity<TestConcurrencyEntity>(modelBuilder);
        ConfigureTestEntity<TestSnakeCaseEntity>(modelBuilder);
    }

    /// <summary>
    /// Adds audit shadow properties and the xmin concurrency token after the snake_case
    /// convention has run, so explicit column names are required.
    /// </summary>
    private static void ConfigureTestEntity<T>(ModelBuilder modelBuilder) where T : class
    {
        var entityType = modelBuilder.Entity<T>();

        entityType.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .IsRequired();
        entityType.Property<DateTime>("UpdatedAt")
            .HasColumnName("updated_at")
            .IsRequired();
        entityType.Property<string>("CreatedBy")
            .HasColumnName("created_by")
            .HasMaxLength(256);
        entityType.Property<string>("UpdatedBy")
            .HasColumnName("updated_by")
            .HasMaxLength(256);

        entityType.Property<uint>("Version")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
    }
}

/// <summary>Entity for AuditInterceptor tests; audit fields are EF shadow properties.</summary>
public class TestAuditableEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

/// <summary>Entity for xmin concurrency tests; Version is an EF shadow property.</summary>
public class TestConcurrencyEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

/// <summary>Entity for snake_case naming convention tests.</summary>
public class TestSnakeCaseEntity
{
    public Guid Id { get; set; }

    public string SomeProperty { get; set; } = string.Empty;

    public Guid NavigationId { get; set; }

    public TestSnakeCaseEntity? Navigation { get; set; }
}
