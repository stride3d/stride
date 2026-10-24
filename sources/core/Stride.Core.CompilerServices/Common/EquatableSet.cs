using System.Collections;
using System.Collections.Frozen;

namespace Stride.Core.CompilerServices.Common;

/// <summary>
/// Represents a set whose equality and hash code are based on its elements rather than their order.
/// </summary>
/// <typeparam name="T">The type of the set elements.</typeparam>
internal readonly struct EquatableSet<T> : IEquatable<EquatableSet<T>>, IReadOnlyCollection<T>
{
    private readonly int _hashCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="EquatableSet{T}"/> struct.
    /// </summary>
    /// <param name="items">The items to include in the set.</param>
    /// <param name="comparer">The comparer used to compare items, or <see langword="null"/> to use the default comparer.</param>
    public EquatableSet(IEnumerable<T> items, IEqualityComparer<T>? comparer = null)
    {
        Items = items.ToFrozenSet(comparer);

        var hashCode = 0;
        unchecked
        {
            foreach (var item in Items)
            {
                hashCode += item is null ? 0 : Items.Comparer.GetHashCode(item);
            }
        }

        _hashCode = hashCode;
    }

    /// <summary>
    /// Gets the frozen set containing the elements.
    /// </summary>
    public FrozenSet<T> Items { get; }

    /// <summary>
    /// Gets the number of elements in the set.
    /// </summary>
    public int Count => Items.Count;

    /// <summary>
    /// Determines whether the set contains the specified item.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns><see langword="true"/> if the set contains <paramref name="item"/>; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T item)
    {
        return Items.Contains(item);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EquatableSet<T> other && Equals(other);
    }

    /// <summary>
    /// Determines whether this set and another set contain the same elements.
    /// </summary>
    /// <param name="other">The set to compare with this set.</param>
    /// <returns><see langword="true"/> if the sets contain the same elements; otherwise, <see langword="false"/>.</returns>
    public bool Equals(EquatableSet<T> other)
    {
        if (ReferenceEquals(Items, other.Items))
            return true;
        if (Items is null || other.Items is null)
            return false;

        return Items.Count == other.Items.Count && _hashCode == other._hashCode && Items.SetEquals(other.Items);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _hashCode;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the set.
    /// </summary>
    /// <returns>An enumerator for the set.</returns>
    public FrozenSet<T>.Enumerator GetEnumerator()
    {
        return Items.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return ((IEnumerable<T>)Items).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Items).GetEnumerator();
    }

    /// <summary>
    /// Determines whether two sets contain the same elements.
    /// </summary>
    /// <param name="left">The first set to compare.</param>
    /// <param name="right">The second set to compare.</param>
    /// <returns><see langword="true"/> if the sets contain the same elements; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(EquatableSet<T> left, EquatableSet<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two sets contain different elements.
    /// </summary>
    /// <param name="left">The first set to compare.</param>
    /// <param name="right">The second set to compare.</param>
    /// <returns><see langword="true"/> if the sets contain different elements; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(EquatableSet<T> left, EquatableSet<T> right)
    {
        return !left.Equals(right);
    }
}
