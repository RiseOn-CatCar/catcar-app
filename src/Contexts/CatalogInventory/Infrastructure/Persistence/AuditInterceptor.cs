using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;

/// <summary>
/// Interceptor that automatically populates audit shadow properties on entity save.
/// AC-008: Audit trail tracking
/// AC-012: Timestamp management
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private const string CreatedAtProperty = "CreatedAt";
    private const string UpdatedAtProperty = "UpdatedAt";
    private const string CreatedByProperty = "CreatedBy";
    private const string UpdatedByProperty = "UpdatedBy";

    /// <summary>
    /// Intercepts SaveChanges to populate audit properties.
    /// AC-008, AC-012
    /// </summary>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context == null)
            return base.SavingChanges(eventData, result);

        PopulateAuditProperties(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Intercepts SaveChangesAsync to populate audit properties.
    /// AC-008, AC-012
    /// </summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context == null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        PopulateAuditProperties(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void PopulateAuditProperties(DbContext context)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var now = DateTime.UtcNow;
        var userId = GetCurrentUserId(context);

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType();

            // Handle CreatedAt
            if (entry.State == EntityState.Added)
            {
                SetShadowProperty(entry, CreatedAtProperty, now);
            }

            // Handle UpdatedAt
            SetShadowProperty(entry, UpdatedAtProperty, now);

            // Handle CreatedBy
            if (entry.State == EntityState.Added)
            {
                SetShadowProperty(entry, CreatedByProperty, userId);
            }

            // Handle UpdatedBy
            SetShadowProperty(entry, UpdatedByProperty, userId);
        }
    }

    private static void SetShadowProperty(EntityEntry entry, string propertyName, object value)
    {
        var property = entry.Property(propertyName);
        property.CurrentValue = value;
        // Mark the property as modified so EF Core includes it in the SQL
        property.IsModified = true;
    }

    private static string GetCurrentUserId(DbContext context)
    {
        // In a real application, this would come from a service context
        // For now, return a default system user
        return "system";
    }
}
