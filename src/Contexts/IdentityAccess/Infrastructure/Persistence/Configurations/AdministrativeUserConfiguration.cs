namespace CatCar.Contexts.IdentityAccess.Infrastructure.Persistence.Configurations;

using CatCar.Contexts.IdentityAccess.Domain.AdministrativeUsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core mapping for the <see cref="AdministrativeUser"/> aggregate.
/// Table/column names are further normalized to snake_case by <see cref="SnakeCaseConvention"/>
/// applied in <see cref="IdentityAccessDbContext.OnModelCreating"/>.
/// </summary>
public sealed class AdministrativeUserConfiguration : IEntityTypeConfiguration<AdministrativeUser>
{
    public void Configure(EntityTypeBuilder<AdministrativeUser> builder)
    {
        builder.ToTable("AdministrativeUsers");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.Email)
            .HasConversion(email => email.Value, value => Email.Create(value).Value)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(u => u.IsActive)
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
