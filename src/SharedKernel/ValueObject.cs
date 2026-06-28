namespace CatCar.SharedKernel;

/// <summary>
/// Base class for value objects with component-based equality.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Returns the components used for equality comparison.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public bool Equals(ValueObject? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (GetType() != other.GetType())
            return false;
        return GetEqualityComponents()
            .Zip(other.GetEqualityComponents(), (a, b) => Equals(a, b))
            .All(x => x);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as ValueObject);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(c => c?.GetHashCode() ?? 0)
            .Aggregate((a, b) => a ^ b);
    }

    /// <summary>
    /// Equality operator for value object component comparison.
    /// </summary>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator for value object component comparison.
    /// </summary>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}
