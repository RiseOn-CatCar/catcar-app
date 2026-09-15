using CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;
using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using CatCar.Persistence;

namespace CatCar.Contexts.CatalogInventory.Infrastructure;

/// <summary>
/// DbContext for the CatalogInventory Bounded Context.
/// AC-008: Schema-per-BC via HasDefaultSchema("catalog_inventory")
/// AC-012: Audit trail shadow properties
/// AC-013: Snake_case naming convention
/// AC-014: xmin concurrency token as uint row version
/// </summary>
public class CatalogInventoryDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the CatalogInventoryDbContext.
    /// </summary>
    public CatalogInventoryDbContext(DbContextOptions<CatalogInventoryDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Services offered by the workshop (feature 03).
    /// </summary>
    public DbSet<CatalogedService> CatalogedServices => Set<CatalogedService>();

    /// <summary>
    /// Parts and supplies managed by the workshop's stock (feature 03).
    /// </summary>
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    /// <summary>
    /// Configures the model for CatalogInventory context.
    /// AC-008: Schema-per-BC
    /// AC-013: Snake_case naming
    /// AC-014: xmin concurrency token
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AC-008: Schema-per-BC - Set default schema for this bounded context
        modelBuilder.HasDefaultSchema("catalog_inventory");

        modelBuilder.ApplyConfiguration(new CatalogedServiceConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryItemConfiguration());

        // AC-013: Apply snake_case naming convention
        SnakeCaseNaming.Apply(modelBuilder);
    }


    /// <summary>
    /// Override SaveChanges to apply audit properties via AuditInterceptor.
    /// AC-008, AC-012
    /// Note: Audit is handled by AuditInterceptor, not here.
    /// </summary>
    public override int SaveChanges()
    {
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to apply audit properties via AuditInterceptor.
    /// AC-008, AC-012
    /// Note: Audit is handled by AuditInterceptor, not here.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    private static string GetCurrentUserId()
    {
        // In a real application, this would come from a service context
        return "system";
    }
}
