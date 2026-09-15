namespace CatCar.Contexts.ServiceOperations.Infrastructure.Persistence.Configurations;

using CatCar.Contexts.ServiceOperations.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core mapping for the <see cref="WorkOrder"/> aggregate, including its owned line-item collections.
/// </summary>
public sealed class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("WorkOrders");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.CustomerId).IsRequired();
        builder.Property(w => w.VehicleId).IsRequired();

        builder.HasIndex(w => w.CustomerId);
        builder.HasIndex(w => w.VehicleId);
        builder.HasIndex(w => w.Status);

        builder.Property(w => w.InitialDescription).HasMaxLength(1000).IsRequired();

        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(w => w.ActiveBudgetId);

        builder.Property(w => w.OpenedAt).IsRequired();
        builder.Property(w => w.DiagnosisStartedAt);
        builder.Property(w => w.BudgetApprovedAt);
        builder.Property(w => w.CompletedAt);
        builder.Property(w => w.DeliveredAt);
        builder.Property(w => w.LastUpdatedAt).IsRequired();

        builder.Metadata.FindNavigation(nameof(WorkOrder.RequestedServices))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsMany(w => w.RequestedServices, service =>
        {
            service.ToTable("WorkOrderRequestedServices");
            service.WithOwner().HasForeignKey("WorkOrderId");
            service.HasKey(s => s.Id);
            service.Property(s => s.Id).ValueGeneratedNever();
            service.Property(s => s.CatalogedServiceId).IsRequired();
            service.Property(s => s.Description).HasMaxLength(500).IsRequired();
            service.Property(s => s.UnitPrice).HasColumnType("numeric(12,2)").IsRequired();
            service.Property(s => s.Quantity).IsRequired();
            service.Ignore(s => s.LineTotal);
        });

        builder.Metadata.FindNavigation(nameof(WorkOrder.RequestedParts))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsMany(w => w.RequestedParts, part =>
        {
            part.ToTable("WorkOrderRequestedParts");
            part.WithOwner().HasForeignKey("WorkOrderId");
            part.HasKey(p => p.Id);
            part.Property(p => p.Id).ValueGeneratedNever();
            part.Property(p => p.InventoryItemId).IsRequired();
            part.Property(p => p.Description).HasMaxLength(500).IsRequired();
            part.Property(p => p.UnitPrice).HasColumnType("numeric(12,2)").IsRequired();
            part.Property(p => p.Quantity).IsRequired();
            part.Ignore(p => p.LineTotal);
        });

        builder.Property<DateTime>("CreatedAt").IsRequired();
        builder.Property<DateTime>("UpdatedAt").IsRequired();
        builder.Property<string>("CreatedBy").HasMaxLength(100).IsRequired();
        builder.Property<string>("UpdatedBy").HasMaxLength(100).IsRequired();

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
    }
}
