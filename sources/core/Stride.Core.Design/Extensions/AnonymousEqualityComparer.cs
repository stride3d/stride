// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Extensions;

/// <summary>
/// This class allows implementation of <see cref="IEqualityComparer{T}"/> using anonymous functions.
/// </summary>
/// <typeparam name="T">The type of object this comparer can compare.</typeparam>
public class AnonymousEqualityComparer<T> : IEqualityComparer<T>
{
    private readonly Func<T?, T?, bool> equals;
    private readonly Func<T, int> getHashCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousEqualityComparer{T}"/> class.
    /// </summary>
    /// <param name="equals">The equality function to use for this equality comparer.</param>
    /// <param name="getHashCode">The function to use to compute hash codes for the objects to compare.</param>
    public AnonymousEqualityComparer(Func<T?, T?, bool> equals, Func<T, int> getHashCode)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(equals);
        ArgumentNullException.ThrowIfNull(getHashCode);
#else
        if (equals is null) throw new ArgumentNullException(nameof(equals));
        if (getHashCode is null) throw new ArgumentNullException(nameof(getHashCode));
#endif
        this.equals = equals;
        this.getHashCode = getHashCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousEqualityComparer{T}"/> class using the default <see cref="object.GetHashCode"/> method to get hash codes.
    /// </summary>
    /// <param name="equals">The equality function to use for this equality comparer.</param>
    public AnonymousEqualityComparer(Func<T?, T?, bool> equals)
        : this(equals, static obj => obj.GetHashCode())
    {
    }

    /// <inheritdoc/>
    public bool Equals(T? x, T? y)
    {
        return equals(x, y);
    }

    /// <inheritdoc/>
    public int GetHashCode(T obj)
    {
        return getHashCode(obj);
    }
}
