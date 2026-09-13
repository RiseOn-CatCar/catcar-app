using CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;
using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using CatCar.Contexts.CatalogInventory.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

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
        ApplySnakeCaseConvention(modelBuilder);
    }

    /// <summary>
    /// Applies snake_case naming convention to all entity types.
    /// AC-013
    /// </summary>
    private static void ApplySnakeCaseConvention(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Convert table name to snake_case
            var tableName = entityType.GetTableName();
            if (tableName != null)
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }

            // Convert column names to snake_case
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            // Convert foreign key column names to snake_case
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreach (var property in foreignKey.Properties)
                {
                    property.SetColumnName(ToSnakeCase(property.Name));
                }
            }
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
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
