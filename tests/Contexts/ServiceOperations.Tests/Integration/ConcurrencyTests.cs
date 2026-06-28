namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using CatCar.Contexts.ServiceOperations.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

// covers: AC-014
public class ConcurrencyTests : IntegrationTestBase
{
    [Fact]
    // covers: AC-014
    public async Task Model_XminConcurrencyToken_MapsToUintRowVersion()
    {
        // Act
        await using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(TestConcurrencyEntity));

        // Assert - Version is a shadow property configured as xmin
        var versionProperty = entityType?.FindProperty("Version");
        versionProperty.Should().NotBeNull();
        versionProperty?.GetColumnName().Should().Be("xmin");
    }

    [Fact]
    // covers: AC-014
    public async Task SaveChanges_ConcurrentUpdate_ThrowsDbUpdateConcurrencyException()
    {
        // Arrange - create entity in context1
        await using var context1 = CreateDbContext();
        var entity = new TestConcurrencyEntity { Name = "Original" };
        context1.TestConcurrencyEntities.Add(entity);
        await context1.SaveChangesAsync();
        var entityId = entity.Id;

        // Act - context2 reads and updates the entity (xmin changes in PostgreSQL)
        await using var context2 = CreateDbContext();
        var entityFromContext2 = await context2.TestConcurrencyEntities.FindAsync(entityId);
        entityFromContext2.Should().NotBeNull();
        entityFromContext2!.Name = "Updated from context2";
        await context2.SaveChangesAsync();

        // Now context1 tries to update the same entity with stale xmin
        // Context1 still has the entity tracked with the original xmin value
        var entityFromContext1 = await context1.TestConcurrencyEntities.FindAsync(entityId);
        entityFromContext1.Should().NotBeNull();
        entityFromContext1!.Name = "Updated from context1";

        // Assert - should throw because xmin changed in PostgreSQL
        await context1.Invoking(c => c.SaveChangesAsync())
            .Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    // covers: AC-014
    public async Task SaveChanges_SingleUpdate_Succeeds()
    {
        // Arrange
        await using var context = CreateDbContext();
        var entity = new TestConcurrencyEntity { Name = "Test" };

        // Act
        context.TestConcurrencyEntities.Add(entity);
        await context.SaveChangesAsync();

        // Assert
        entity.Id.Should().NotBeEmpty();
    }
}
