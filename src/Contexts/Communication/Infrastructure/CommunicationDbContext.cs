using CatCar.Contexts.Communication.Domain.ExternalAccessTokens;
using CatCar.Contexts.Communication.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

using CatCar.Persistence;
namespace CatCar.Contexts.Communication.Infrastructure;

/// <summary>
/// DbContext for the Communication Bounded Context.
/// AC-008: Schema-per-BC via HasDefaultSchema("communication")
/// AC-012: Audit trail shadow properties
/// AC-013: Snake_case naming convention
/// AC-014: xmin concurrency token as uint row version
/// </summary>
public class CommunicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the CommunicationDbContext.
    /// </summary>
    public CommunicationDbContext(DbContextOptions<CommunicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// External, single-use approval-link tokens issued for a Budget (feature 05).
    /// </summary>
    public DbSet<ExternalAccessToken> ExternalAccessTokens => Set<ExternalAccessToken>();

    /// <summary>
    /// Configures the model for Communication context.
    /// AC-008: Schema-per-BC
    /// AC-013: Snake_case naming
    /// AC-014: xmin concurrency token
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AC-008: Schema-per-BC - Set default schema for this bounded context
        modelBuilder.HasDefaultSchema("communication");

        modelBuilder.ApplyConfiguration(new ExternalAccessTokenConfiguration());

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
