using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using CatCar.Persistence;

namespace CatCar.Contexts.ServiceOperations.Infrastructure;

/// <summary>
/// DbContext for the ServiceOperations Bounded Context.
/// AC-008: Schema-per-BC via HasDefaultSchema("service_operations")
/// AC-013: Audit shadow properties (created_at, updated_at) via AuditInterceptor
/// AC-014: xmin concurrency token as uint row version
/// </summary>
public class ServiceOperationsDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the ServiceOperationsDbContext.
    /// </summary>
    public ServiceOperationsDbContext(DbContextOptions<ServiceOperationsDbContext> options)
        : base(options)
    {
    }

    // Test entity sets for integration testing
    public DbSet<TestAuditableEntity> TestAuditableEntities { get; set; } = null!;
    public DbSet<TestConcurrencyEntity> TestConcurrencyEntities { get; set; } = null!;
    public DbSet<TestSnakeCaseEntity> TestSnakeCaseEntities { get; set; } = null!;

    /// <summary>
    /// Customers identified by CPF/CNPJ (feature 04).
    /// </summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>
    /// Customer vehicles (feature 04).
    /// </summary>
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    /// <summary>
    /// Work orders (OS) opened for a customer's vehicle (feature 04).
    /// </summary>
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    /// <summary>
    /// Budgets issued for a work order (feature 04).
    /// </summary>
    public DbSet<Budget> Budgets => Set<Budget>();

    /// <summary>
    /// Configures the model for ServiceOperations context.
    /// AC-008: Schema-per-BC
    /// AC-013: Snake_case naming
    /// AC-014: xmin concurrency token
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AC-008: Schema-per-BC - Set default schema for this bounded context
        modelBuilder.HasDefaultSchema("service_operations");

        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new VehicleConfiguration());
        modelBuilder.ApplyConfiguration(new WorkOrderConfiguration());
        modelBuilder.ApplyConfiguration(new BudgetConfiguration());

        // AC-013: Apply snake_case naming convention
        SnakeCaseNaming.Apply(modelBuilder);

        // Configure test entities for integration tests
        ConfigureTestEntity<TestAuditableEntity>(modelBuilder);
        ConfigureTestEntity<TestConcurrencyEntity>(modelBuilder);
        ConfigureTestEntity<TestSnakeCaseEntity>(modelBuilder);
    }


    /// <summary>
    /// Configures an entity with audit shadow properties and xmin concurrency token.
    /// AC-008, AC-013, AC-014
    /// Shadow properties are added after SnakeCaseNaming.Apply so explicit
    /// HasColumnName calls are required to enforce snake_case column names.
    /// </summary>
    private static void ConfigureTestEntity<T>(ModelBuilder modelBuilder) where T : class
    {
        var entityType = modelBuilder.Entity<T>();

        // AC-013: Audit shadow properties with explicit snake_case column names.
        // These properties are added after SnakeCaseNaming.Apply, so explicit
        // HasColumnName calls are required to preserve snake_case names.
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

        // AC-014: xmin concurrency token as uint row version.
        // HasColumnType("xid") maps to PostgreSQL's xmin system column type.
        entityType.Property<uint>("Version")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
    }
}

/// <summary>
/// Test entity with audit shadow properties.
/// Used for integration testing of AuditInterceptor.
/// AC-008, AC-012
/// Note: Audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) are EF shadow properties.
/// </summary>
public class TestAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // Audit fields are EF shadow properties configured in ConfigureTestEntity
}

/// <summary>
/// Test entity with xmin concurrency token.
/// Used for integration testing of concurrency configuration.
/// AC-014
/// Note: Version is an EF shadow property configured in ConfigureTestEntity.
/// </summary>
public class TestConcurrencyEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // Version (xmin) is an EF shadow property configured in ConfigureTestEntity
}

/// <summary>
/// Test entity for snake_case naming convention verification.
/// AC-013
/// </summary>
public class TestSnakeCaseEntity
{
    public Guid Id { get; set; }
    public string SomeProperty { get; set; } = string.Empty;
    public Guid NavigationId { get; set; }
    public TestSnakeCaseEntity? Navigation { get; set; }
}
