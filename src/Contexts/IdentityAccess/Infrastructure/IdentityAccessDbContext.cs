using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using CatCar.Contexts.IdentityAccess.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

using CatCar.Persistence;
namespace CatCar.Contexts.IdentityAccess.Infrastructure;

/// <summary>
/// DbContext for the IdentityAccess Bounded Context.
/// AC-008: Schema-per-BC via HasDefaultSchema("identity_access")
/// AC-012: Audit trail shadow properties
/// AC-013: Snake_case naming convention
/// AC-014: xmin concurrency token as uint row version
/// </summary>
public class IdentityAccessDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the IdentityAccessDbContext.
    /// </summary>
    public IdentityAccessDbContext(DbContextOptions<IdentityAccessDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Administrative panel access credentials (feature 02 - identidade-acesso-administrativo).
    /// </summary>
    public DbSet<AdministrativeUser> AdministrativeUsers => Set<AdministrativeUser>();

    /// <summary>
    /// Configures the model for IdentityAccess context.
    /// AC-008: Schema-per-BC
    /// AC-013: Snake_case naming
    /// AC-014: xmin concurrency token
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AC-008: Schema-per-BC - Set default schema for this bounded context
        modelBuilder.HasDefaultSchema("identity_access");

        modelBuilder.ApplyConfiguration(new AdministrativeUserConfiguration());

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
