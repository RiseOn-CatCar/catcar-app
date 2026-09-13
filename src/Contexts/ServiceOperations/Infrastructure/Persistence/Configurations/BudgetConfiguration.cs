namespace CatCar.Contexts.ServiceOperations.Infrastructure.Persistence.Configurations;

using CatCar.Contexts.ServiceOperations.Domain.Budgets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core mapping for the <see cref="Budget"/> aggregate, including its owned frozen line-item collection.
/// </summary>
public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("Budgets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.WorkOrderId).IsRequired();
        builder.HasIndex(b => b.WorkOrderId);

        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(b => b.IssuedAt).IsRequired();

        builder.Ignore(b => b.TotalAmount);

        builder.Metadata.FindNavigation(nameof(Budget.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsMany(b => b.Lines, line =>
        {
            line.ToTable("BudgetLines");
            line.WithOwner().HasForeignKey("BudgetId");
            line.HasKey(l => l.Id);
            line.Property(l => l.Id).ValueGeneratedNever();
            line.Property(l => l.Type).HasConversion<string>().HasMaxLength(10).IsRequired();
            line.Property(l => l.ReferenceId).IsRequired();
            line.Property(l => l.Description).HasMaxLength(500).IsRequired();
            line.Property(l => l.UnitPrice).HasColumnType("numeric(12,2)").IsRequired();
            line.Property(l => l.Quantity).IsRequired();
            line.Ignore(l => l.LineTotal);
        });

        builder.Property<DateTime>("CreatedAt").IsRequired();
        builder.Property<DateTime>("UpdatedAt").IsRequired();
        builder.Property<string>("CreatedBy").HasMaxLength(100).IsRequired();
        builder.Property<string>("UpdatedBy").HasMaxLength(100).IsRequired();

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
    }
}
