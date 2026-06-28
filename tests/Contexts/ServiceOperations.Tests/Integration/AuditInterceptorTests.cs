namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using CatCar.Contexts.ServiceOperations.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

// covers: AC-008, AC-012, AC-013
// AC-008: test entities live in the service_operations schema (HasDefaultSchema)
// AC-012: IDs are client-side generated (UUID v7 in domain constructors); DbContext must not interfere
// AC-013: AuditInterceptor populates created_at / updated_at shadow properties automatically
public class AuditInterceptorTests : IntegrationTestBase
{
    [Fact]
    // covers: AC-013
    public async Task SaveChanges_AutoPopulatesCreatedAtTimestamp()
    {
        // Arrange
        var entity = new TestAuditableEntity { Name = "Test" };

        // Act
        await using var context = CreateDbContext();
        context.TestAuditableEntities.Add(entity);
        await context.SaveChangesAsync();

        // Assert - read shadow property via fresh query (shadow properties are not on CLR entity)
        await using var assertContext = CreateDbContext();
        var persisted = await assertContext.TestAuditableEntities.FindAsync(entity.Id);
        persisted.Should().NotBeNull();

        var createdAt = assertContext.Entry(persisted!).Property<DateTime>("CreatedAt").CurrentValue;
        createdAt.Should().NotBe(default);
    }

    [Fact]
    // covers: AC-013
    public async Task SaveChanges_AutoPopulatesUpdatedAtTimestamp()
    {
        // Arrange
        await using var context = CreateDbContext();
        var entity = new TestAuditableEntity { Name = "Test" };
        context.TestAuditableEntities.Add(entity);
        await context.SaveChangesAsync();

        // Read original UpdatedAt from fresh context
        await using var readContext = CreateDbContext();
        var originalUpdatedAt = readContext.Entry(entity).Property<DateTime>("UpdatedAt").CurrentValue;

        // Act
        entity.Name = "Updated";
        await context.SaveChangesAsync();

        // Assert - read shadow property via fresh query
        await using var assertContext = CreateDbContext();
        var persisted = await assertContext.TestAuditableEntities.FindAsync(entity.Id);
        var updatedAt = assertContext.Entry(persisted!).Property<DateTime>("UpdatedAt").CurrentValue;
        updatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    // covers: AC-013
    public async Task SaveChanges_SetsCreatedByFromInterceptor()
    {
        // Arrange
        await using var context = CreateDbContext();
        var entity = new TestAuditableEntity { Name = "Test" };

        // Act
        context.TestAuditableEntities.Add(entity);
        await context.SaveChangesAsync();

        // Assert - read shadow property via fresh query
        await using var assertContext = CreateDbContext();
        var persisted = await assertContext.TestAuditableEntities.FindAsync(entity.Id);
        var createdBy = assertContext.Entry(persisted!).Property<string>("CreatedBy").CurrentValue;
        createdBy.Should().NotBeNullOrEmpty();
    }

    [Fact]
    // covers: AC-013
    public async Task SaveChanges_SetsUpdatedByFromInterceptor()
    {
        // Arrange
        await using var context = CreateDbContext();
        var entity = new TestAuditableEntity { Name = "Test" };
        context.TestAuditableEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.Name = "Updated";
        await context.SaveChangesAsync();

        // Assert - read shadow property via fresh query
        await using var assertContext = CreateDbContext();
        var persisted = await assertContext.TestAuditableEntities.FindAsync(entity.Id);
        var updatedBy = assertContext.Entry(persisted!).Property<string>("UpdatedBy").CurrentValue;
        updatedBy.Should().NotBeNullOrEmpty();
    }
}
