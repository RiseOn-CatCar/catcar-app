using CatCar.Contexts.ServiceOperations.Domain.Customers;
using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using CatCar.Contexts.ServiceOperations.Features.WorkOrders.Saga;
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

    /// <summary>
    /// Allows a test-only derived context to reuse the production model.
    /// </summary>
    protected ServiceOperationsDbContext(DbContextOptions options)
        : base(options)
    {
    }

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
        modelBuilder.Entity<WorkOrderSaga>(saga =>
        {
            saga.ToTable("WorkOrderSagas");
            saga.HasKey(x => x.Id);
            saga.Property(x => x.Id).ValueGeneratedNever();
            saga.Property(x => x.State).IsRequired();
            saga.Property(x => x.Amount).HasColumnType("numeric(12,2)");
            saga.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
            saga.Ignore(x => x.Version);
        });

        // AC-013: Apply snake_case naming convention
        SnakeCaseNaming.Apply(modelBuilder);
    }
}
