namespace CatCar.Contexts.CatalogInventory.Infrastructure.Persistence.Configurations;

using CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core mapping for the <see cref="CatalogedService"/> aggregate.
/// Table/column names are further normalized to snake_case by
/// <see cref="CatalogInventoryDbContext.OnModelCreating"/>.
/// </summary>
public sealed class CatalogedServiceConfiguration : IEntityTypeConfiguration<CatalogedService>
{
    public void Configure(EntityTypeBuilder<CatalogedService> builder)
    {
        builder.ToTable("CatalogedServices");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(s => s.Name).IsUnique();

        builder.Property(s => s.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.EstimatedDurationMinutes)
            .IsRequired();

        builder.OwnsOne(s => s.Price, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnType("numeric(12,2)")
                .IsRequired();

            price.Property(p => p.Currency)
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(s => s.Price).IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired();

        // AC-012: audit trail shadow properties (populated by AuditInterceptor).
        builder.Property<DateTime>("CreatedAt").IsRequired();
        builder.Property<DateTime>("UpdatedAt").IsRequired();
        builder.Property<string>("CreatedBy").HasMaxLength(100).IsRequired();
        builder.Property<string>("UpdatedBy").HasMaxLength(100).IsRequired();

        // AC-014: xmin concurrency token as uint row version.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion();
    }
}
