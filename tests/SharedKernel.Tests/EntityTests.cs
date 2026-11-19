namespace CatCar.SharedKernel.Tests;

using CatCar.SharedKernel;
using FluentAssertions;
using Xunit;

public class EntityTests
{
    // covers: AC-007
    [Fact]
    public void Entity_SameId_AreEqual()
    {
        // Arrange
        var id = new TestId(1);
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        entity1.Should().Be(entity2);
    }

    // covers: AC-007
    [Fact]
    public void Entity_DifferentId_AreNotEqual()
    {
        // Arrange
        var entity1 = new TestEntity(new TestId(1));
        var entity2 = new TestEntity(new TestId(2));

        // Act & Assert
        entity1.Should().NotBe(entity2);
    }

    // covers: AC-007
    [Fact]
    public void Entity_NullId_AreNotEqual()
    {
        // Arrange
        var entity1 = new TestEntity(new TestId(1));
        var entity2 = new TestEntity(null!);

        // Act & Assert
        entity1.Should().NotBe(entity2);
    }

    // covers: AC-007
    [Fact]
    public void Entity_EqualsOperator_SameId_ReturnsTrue()
    {
        // Arrange
        var id = new TestId(42);
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        (entity1 == entity2).Should().BeTrue();
    }

    // covers: AC-007
    [Fact]
    public void Entity_NotEqualsOperator_DifferentId_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestEntity(new TestId(1));
        var entity2 = new TestEntity(new TestId(2));

        // Act & Assert
        (entity1 != entity2).Should().BeTrue();
    }

    // covers: AC-007
    [Fact]
    public void Entity_GetHashCode_SameId_SameHash()
    {
        // Arrange
        var id = new TestId(99);
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }
    [Fact]
    public void Equals_NullEntity_ReturnsFalse()
    {
        new TestEntity(new TestId(1)).Equals((Entity<TestId>?)null).Should().BeFalse();
    }

    [Fact]
    public void Equals_NullObject_ReturnsFalse()
    {
        new TestEntity(new TestId(1)).Equals((object?)null).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameInstanceAndUnrelatedObject_RespectIdentity()
    {
        var entity = new TestEntity(new TestId(1));
        entity.Equals((object)entity).Should().BeTrue();
        entity.Equals(new object()).Should().BeFalse();
        Entity<TestId>? absent = null;
        (absent == null).Should().BeTrue();
        (absent != entity).Should().BeTrue();
    }

    [Fact]
    public void Equals_ObjectOfDifferentEntityType_ReturnsFalse()
    {
        var id = new TestId(1);
        new TestEntity(id).Equals((object)new OtherTestEntity(id)).Should().BeFalse();
    }
}

// Test helper types for Entity<TId> verification
internal sealed record TestId(int Value);

internal sealed class TestEntity : Entity<TestId>
{
    public TestEntity(TestId id) : base(id) { }
}

internal sealed class OtherTestEntity : Entity<TestId>
{
    public OtherTestEntity(TestId id) : base(id) { }
}
