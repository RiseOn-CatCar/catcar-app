namespace CatCar.Contexts.ServiceOperations.Infrastructure.Persistence.Configurations;

using CatCar.Contexts.ServiceOperations.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core mapping for the <see cref="Vehicle"/> aggregate.
/// </summary>
public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.CustomerId).IsRequired();

        builder.HasIndex(v => v.CustomerId);

        builder.OwnsOne(v => v.Plate, plate =>
        {
            plate.Property(p => p.Value)
                .HasColumnName("Plate")
                .HasMaxLength(7)
                .IsRequired();

            plate.HasIndex(p => p.Value).IsUnique();
        });

        builder.Navigation(v => v.Plate).IsRequired();

        builder.Property(v => v.Brand).HasMaxLength(60).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(60).IsRequired();
        builder.Property(v => v.ManufactureYear).IsRequired();
        builder.Property(v => v.IsActive).IsRequired();

        builder.Property<DateTime>("CreatedAt").IsRequired();
        builder.Property<DateTime>("UpdatedAt").IsRequired();
        builder.Property<string>("CreatedBy").HasMaxLength(100).IsRequired();
        builder.Property<string>("UpdatedBy").HasMaxLength(100).IsRequired();

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
    }
}
