namespace CatCar.Contexts.CatalogInventory.Infrastructure.Persistence.Configurations;

using CatCar.Contexts.CatalogInventory.Domain.InventoryItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core mapping for the <see cref="InventoryItem"/> aggregate.
/// Table/column names are further normalized to snake_case by
/// <see cref="CatalogInventoryDbContext.OnModelCreating"/>.
/// </summary>
public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.Sku)
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(i => i.Sku).IsUnique();

        builder.Property(i => i.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(i => i.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.OwnsOne(i => i.UnitPrice, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnType("numeric(12,2)")
                .IsRequired();

            price.Property(p => p.Currency)
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(i => i.UnitPrice).IsRequired();

        builder.Property(i => i.QuantityInStock)
            .IsRequired();

        builder.Property(i => i.MinimumStockThreshold)
            .IsRequired();

        builder.Ignore(i => i.IsBelowMinimumStock);

        builder.Property(i => i.IsActive)
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
