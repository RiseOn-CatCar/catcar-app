namespace CatCar.Contexts.CatalogInventory.Infrastructure.Persistence.Configurations;

using CatCar.Contexts.CatalogInventory.Domain.InventoryReservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class BudgetReservationOutcomeConfiguration : IEntityTypeConfiguration<BudgetReservationOutcome>
{
    public void Configure(EntityTypeBuilder<BudgetReservationOutcome> builder)
    {
        builder.ToTable("BudgetReservationOutcomes");
        builder.HasKey(x => x.BudgetId).HasName("pk_budget_reservation_outcomes");
        builder.Property(x => x.BudgetId).ValueGeneratedNever();
        builder.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.FailureCode).HasConversion<string>().HasMaxLength(50);
        builder.Property<DateTime>("CreatedAt").IsRequired();
        builder.Property<DateTime>("UpdatedAt").IsRequired();
        builder.Property<string>("CreatedBy").HasMaxLength(100).IsRequired();
        builder.Property<string>("UpdatedBy").HasMaxLength(100).IsRequired();
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
    }
}
