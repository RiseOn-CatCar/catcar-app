namespace CatCar.Contexts.Communication.Infrastructure.Persistence.Configurations;

using CatCar.Contexts.Communication.Domain.ExternalAccessTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core mapping for the <see cref="ExternalAccessToken"/> aggregate.
/// </summary>
public sealed class ExternalAccessTokenConfiguration : IEntityTypeConfiguration<ExternalAccessToken>
{
    public void Configure(EntityTypeBuilder<ExternalAccessToken> builder)
    {
        builder.ToTable("ExternalAccessTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.BudgetId).IsRequired();
        builder.HasIndex(t => t.BudgetId);

        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(t => t.IssuedAt).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.ConsumedAt);

        builder.Property<DateTime>("CreatedAt").IsRequired();
        builder.Property<DateTime>("UpdatedAt").IsRequired();
        builder.Property<string>("CreatedBy").HasMaxLength(100).IsRequired();
        builder.Property<string>("UpdatedBy").HasMaxLength(100).IsRequired();

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
    }
}
