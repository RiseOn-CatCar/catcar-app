namespace CatCar.Contexts.CatalogInventory.Infrastructure.Persistence.Configurations;

using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("InventoryReservations");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.WorkOrderId).IsRequired();
        builder.Property(r => r.BudgetId).IsRequired();
        builder.Property(r => r.InventoryItemId).IsRequired();
        builder.Property(r => r.Quantity).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(r => r.ReservedAt).IsRequired();
        builder.Property(r => r.ConsumedAt);
        builder.Property(r => r.ReleasedAt);

        builder.Property<DateTime>("CreatedAt").IsRequired();
        builder.Property<DateTime>("UpdatedAt").IsRequired();
        builder.Property<string>("CreatedBy").HasMaxLength(100).IsRequired();
        builder.Property<string>("UpdatedBy").HasMaxLength(100).IsRequired();

        builder.HasIndex(r => r.WorkOrderId);
        builder.HasIndex(r => r.BudgetId);
        builder.HasIndex(r => new { r.BudgetId, r.InventoryItemId }).IsUnique();

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion();
    }
}
