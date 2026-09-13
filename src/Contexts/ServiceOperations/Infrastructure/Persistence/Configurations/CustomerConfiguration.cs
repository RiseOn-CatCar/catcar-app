namespace CatCar.Contexts.ServiceOperations.Infrastructure.Persistence.Configurations;

using CatCar.Contexts.ServiceOperations.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core mapping for the <see cref="Customer"/> aggregate.
/// </summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.OwnsOne(c => c.Document, document =>
        {
            document.Property(d => d.Value)
                .HasColumnName("DocumentNumber")
                .HasMaxLength(14)
                .IsRequired();

            document.Property(d => d.Type)
                .HasColumnName("DocumentType")
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();

            document.HasIndex(d => d.Value).IsUnique();
        });

        builder.Navigation(c => c.Document).IsRequired();

        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.IsActive).IsRequired();

        builder.Property<DateTime>("CreatedAt").IsRequired();
        builder.Property<DateTime>("UpdatedAt").IsRequired();
        builder.Property<string>("CreatedBy").HasMaxLength(100).IsRequired();
        builder.Property<string>("UpdatedBy").HasMaxLength(100).IsRequired();

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
    }
}
