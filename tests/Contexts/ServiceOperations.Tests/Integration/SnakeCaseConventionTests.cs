namespace CatCar.Contexts.ServiceOperations.Tests.Integration;

using CatCar.Contexts.ServiceOperations.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

// covers: AC-013
public class SnakeCaseConventionTests : IntegrationTestBase
{
    [Fact]
    // covers: AC-013
    public async Task Model_EntityProperty_ReturnsSnakeCaseColumnName()
    {
        // Act
        await using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(TestSnakeCaseEntity));

        // Assert
        var columnName = entityType?.FindProperty(nameof(TestSnakeCaseEntity.SomeProperty))?.GetColumnName();
        columnName.Should().Be("some_property");
    }

    [Fact]
    // covers: AC-013
    public async Task Model_RelationalTable_ReturnsSnakeCaseTableName()
    {
        // Act
        await using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(TestSnakeCaseEntity));

        // Assert - EF Core pluralizes table names by default
        var tableName = entityType?.GetTableName();
        tableName.Should().Be("test_snake_case_entities");
    }

    [Fact]
    // covers: AC-013
    public async Task Model_NavigationProperty_ReturnsSnakeCaseForeignKeyColumnName()
    {
        // Act
        await using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(TestSnakeCaseEntity));

        // Assert
        var fkProperty = entityType?.FindProperty(nameof(TestSnakeCaseEntity.NavigationId));
        fkProperty?.GetColumnName().Should().Be("navigation_id");
    }

    [Fact]
    // covers: AC-013
    public async Task Model_AuditShadowProperties_HaveSnakeCaseColumnNames()
    {
        // AC-013: created_at and updated_at columns must use snake_case naming
        // so the domain model stays clean and DB schema uses consistent conventions
        await using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(TestAuditableEntity));

        var createdAtColumn = entityType?.FindProperty("CreatedAt")?.GetColumnName();
        var updatedAtColumn = entityType?.FindProperty("UpdatedAt")?.GetColumnName();

        createdAtColumn.Should().Be("created_at");
        updatedAtColumn.Should().Be("updated_at");
    }
}
