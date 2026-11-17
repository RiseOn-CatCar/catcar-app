namespace CatCar.SharedKernel.Tests;

using CatCar.SharedKernel;
using FluentAssertions;
using Xunit;

public class ValueObjectTests
{
    // covers: AC-007
    [Fact]
    public void ValueObject_AllComponentsEqual_AreEqual()
    {
        // Arrange
        var vo1 = new TestValueObject("foo", 42);
        var vo2 = new TestValueObject("foo", 42);

        // Act & Assert
        vo1.Should().Be(vo2);
    }

    // covers: AC-007
    [Fact]
    public void ValueObject_AnyComponentDifferent_AreNotEqual()
    {
        // Arrange
        var vo1 = new TestValueObject("foo", 42);
        var vo2 = new TestValueObject("foo", 43);
        var vo3 = new TestValueObject("bar", 42);

        // Act & Assert
        vo1.Should().NotBe(vo2);
        vo1.Should().NotBe(vo3);
    }

    // covers: AC-007
    [Fact]
    public void ValueObject_EqualsOperator_SameComponents_ReturnsTrue()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 100);
        var vo2 = new TestValueObject("test", 100);

        // Act & Assert
        (vo1 == vo2).Should().BeTrue();
    }

    // covers: AC-007
    [Fact]
    public void ValueObject_NotEqualsOperator_DifferentComponents_ReturnsTrue()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 100);
        var vo2 = new TestValueObject("test", 200);

        // Act & Assert
        (vo1 != vo2).Should().BeTrue();
    }

    // covers: AC-007
    [Fact]
    public void ValueObject_GetHashCode_SameComponents_SameHash()
    {
        // Arrange
        var vo1 = new TestValueObject("hash", 777);
        var vo2 = new TestValueObject("hash", 777);

        // Act & Assert
        vo1.GetHashCode().Should().Be(vo2.GetHashCode());
    }

    // covers: AC-007
    [Fact]
    public void ValueObject_GetHashCode_DifferentComponents_DifferentHash()
    {
        // Arrange - collision is possible but unlikely with these values
        var vo1 = new TestValueObject("a", 1);
        var vo2 = new TestValueObject("b", 2);

        // Act & Assert
        vo1.GetHashCode().Should().NotBe(vo2.GetHashCode());
    }
    [Fact]
    public void Equals_NullValueObject_ReturnsFalse()
    {
        new TestValueObject("test", 1).Equals((ValueObject?)null).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameInstanceAndUnrelatedObject_RespectValueSemantics()
    {
        var value = new TestValueObject("test", 1);
        value.Equals((object)value).Should().BeTrue();
        value.Equals(new object()).Should().BeFalse();
        ValueObject? absent = null;
        (absent == null).Should().BeTrue();
        (absent != value).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentValueObjectType_ReturnsFalse()
    {
        new TestValueObject("test", 1).Equals(new OtherValueObject()).Should().BeFalse();
    }
}

// Test helper type for ValueObject verification
internal sealed class TestValueObject : ValueObject
{
    public string Name { get; }
    public int Count { get; }

    public TestValueObject(string name, int count)
    {
        Name = name;
        Count = count;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Count;
    }
}

internal sealed class OtherValueObject : ValueObject
{
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return "test";
        yield return 1;
    }
}
