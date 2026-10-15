using System.Collections;

namespace Stride.Core.CompilerServices.Common;

internal readonly struct EquatableArray<T>(T[] array) : IEquatable<EquatableArray<T>>, IReadOnlyList<T> where T : IEquatable<T>
{
    private readonly T[] _array = array;

    public T this[int index] { get => _array[index]; }

    public int Count => _array.Length;

    public static EquatableArray<T> Empty => new([]);

    public bool Contains(T item)
    {
        return _array.Contains(item);
    }

    /// <summary>
    /// Copies the elements to an array, starting at the specified destination index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based destination index.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        _array.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && Equals(other);
    }

    /// <summary>
    /// Determines whether this array and another array contain equal elements in the same order.
    /// </summary>
    /// <param name="other">The array to compare with this array.</param>
    /// <returns><see langword="true"/> if the arrays are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(EquatableArray<T> other)
    {
        if (_array.Length != other._array.Length)
            return false;
        for (int i = 0; i < _array.Length; i++)
        {
            if (!_array[i].Equals(other._array[i]))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the array.
    /// </summary>
    /// <returns>An enumerator for the array.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)_array).GetEnumerator();
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var item in _array)
        {
            hash = unchecked(hash * 31 + (item?.GetHashCode() ?? 0));
        }
        return hash;
    }

    /// <summary>
    /// Searches for the specified item and returns its zero-based index.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns>The zero-based index of <paramref name="item"/>, or -1 if it is not found.</returns>
    public int IndexOf(T item)
    {
        return _array.IndexOf(item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _array.GetEnumerator();
    }

    /// <summary>
    /// Converts an array to an equatable array.
    /// </summary>
    /// <param name="array">The array to convert.</param>
    public static implicit operator EquatableArray<T>(T[] array)
    {
        return new EquatableArray<T>(array);
    }

    /// <summary>
    /// Determines whether two arrays contain equal elements in the same order.
    /// </summary>
    /// <param name="left">The first array to compare.</param>
    /// <param name="right">The second array to compare.</param>
    /// <returns><see langword="true"/> if the arrays are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two arrays contain different elements or elements in a different order.
    /// </summary>
    /// <param name="left">The first array to compare.</param>
    /// <param name="right">The second array to compare.</param>
    /// <returns><see langword="true"/> if the arrays are different; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
    {
        return !left.Equals(right);
    }
}
