using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.Core;

internal static class StrideCoreExtensions
{
    /// <summary>Determines whether two sequences are equal. Comparing the elements is done using the default equality comparer for their type.
    /// <para>Allows either parameter to be <c>null</c>.</para>
    /// <para>A thin wrapper around <see cref="Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>.</para></summary>
    /// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
    /// <param name="first">An enumerable to compare to <paramref name="second"/>.</param>
    /// <param name="second">An enumerable to compare to <paramref name="first"/>.</param>
    /// <returns><c>true</c> if one of the following is true.
    /// <list type="bullet">
    /// <item><paramref name="first"/> and <paramref name="second"/> are the same object.</item>
    /// <item>Neither enumerable is <c>null</c> and they have the same length and each of the elements in the enumerables compare equal pairwise.</item>
    /// </list>
    /// <para><c>false</c> otherwise.</para></returns>
    public static bool SequenceEqualAllowNull<T>(this IEnumerable<T> first, IEnumerable<T> second)
        => SequenceEqualAllowNull(first, second, null);

    /// <summary>Determines whether two sequences are equal. Comparing the elements is done using the specified equality comparer.
    /// <para>Allows <paramref name="first"/> and/or <paramref name="second"/> to be <c>null</c>.</para>
    /// <para>A thin wrapper around <see cref="Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource}, IEqualityComparer{TSource}?)"/>.</para></summary>
    /// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
    /// <param name="first">An enumerable to compare to <paramref name="second"/>.</param>
    /// <param name="second">An enumerable to compare to <paramref name="first"/>.</param>
    /// <param name="comparer">The equality comparer.</param>
    /// <returns><c>true</c> if one of the following is true.
    /// <list type="bullet">
    /// <item><paramref name="first"/> and <paramref name="second"/> are the same object.</item>
    /// <item>Neither enumerable is <c>null</c> and they have the same length and each of the elements in the enumerables compare equal pairwise.</item>
    /// </list>
    /// <para><c>false</c> otherwise.</para></returns>
    public static bool SequenceEqualAllowNull<T>(this IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T> comparer)
    {
        if (ReferenceEquals(first, second)) return true;
        if (first is null || second is null) return false;
        if (first is List<T> llist && second is List<T> rlist)
        {
            var lhs = CollectionsMarshal.AsSpan(llist);
            var rhs = CollectionsMarshal.AsSpan(rlist);
            return lhs.SequenceEqual(rhs);
        }
        return Enumerable.SequenceEqual(first, second, comparer);
    }
}
