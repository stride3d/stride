// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.Quantum;

/// <summary>
/// A container structure to represent indices in Quantum nodes.
/// </summary>
public readonly struct NodeIndex : IEquatable<NodeIndex>, IComparable<NodeIndex>, IComparable
{
    /// <summary>
    /// An index that is null.
    /// </summary>
    public static readonly NodeIndex Empty = new();

    /// <summary>
    /// The value of the index.
    /// </summary>
    public readonly object? Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeIndex"/> structure.
    /// </summary>
    /// <param name="value">The value of the index.</param>
    public NodeIndex(object? value) : this()
    {
        // Sanity check, to avoid boxing index into index
        if (value is NodeIndex) throw new ArgumentException($"A {nameof(NodeIndex)} instance cannot be passed as the value of another {nameof(NodeIndex)} instance.");
        Value = value;
    }

    /// <summary>
    /// Gets whether this index is empty.
    /// </summary>
    public readonly bool IsEmpty => Value is null;

    /// <summary>
    /// Gets whether this index is an integer.
    /// </summary>
    public readonly bool IsInt => Value is int;

    /// <summary>
    /// Gets the integer value of this index.
    /// </summary>
    /// <exception cref="InvalidCastException">The value of this index is not an integer.</exception>
    public readonly int Int => Value is int i ? i : throw new InvalidCastException();

    /// <summary>
    /// Gets the value of this index.
    /// </summary>
    /// <param name="value">When this method returns, contains the value of this index.</param>
    /// <returns><c>true</c> if the index is not empty; otherwise, <c>false</c>.</returns>
    public readonly bool TryGetValue([MaybeNullWhen(false)] out object value)
    {
        value = Value;
        return value is not null;
    }

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        return Value?.ToString() ?? "(null)";
    }

    /// <inheritdoc/>
    public readonly bool Equals(NodeIndex other)
    {
        return Equals(Value, other.Value);
    }

    /// <inheritdoc/>
    public readonly int CompareTo(NodeIndex other)
    {
        if (other.IsEmpty)
            return IsEmpty ? 0 : 1;
        if (Value is not IComparable thisValue)
            return other.Value is IComparable ? -1 : 0;

        return thisValue.CompareTo(other.Value);
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is NodeIndex nodeIndex && Equals(nodeIndex);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return Value?.GetHashCode() ?? 0;
    }

    /// <inheritdoc/>
    readonly int IComparable.CompareTo(object? obj)
    {
        if (obj is not NodeIndex nodeIndex)
            throw new ArgumentException("obj is not the same type as this instance.", nameof(obj));

        return CompareTo(nodeIndex);
    }

    public static bool operator ==(NodeIndex a, NodeIndex b)
    {
        return Equals(a.Value, b.Value);
    }

    /// <inheritdoc/>
    public static bool operator !=(NodeIndex a, NodeIndex b)
    {
        return !Equals(a.Value, b.Value);
    }
}
