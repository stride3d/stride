// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections;
using System.Diagnostics;

namespace Stride.Core;

/// <summary>
///   Provides a set of static utility methods for collection operations, and general-purpose helpers.
/// </summary>
public static class Utilities
{
    /// <summary>
    ///   Disposes an <see cref="IDisposable"/> object if not <see langword="null"/>,
    ///   and sets it to <see langword="null"/>.
    /// </summary>
    /// <param name="disposable">The disposable object to dispose.</param>
    public static void Dispose<T>(ref T? disposable) where T : class, IDisposable
    {
        disposable?.Dispose();
        disposable = null;
    }

    /// <summary>
    ///   Read stream to a byte[] buffer
    /// </summary>
    /// <param name = "stream">input stream</param>
    /// <returns>a byte[] buffer</returns>
    [Obsolete("Allocates. Read into the destination.")]
    public static byte[] ReadStream(Stream stream)
    {
        Debug.Assert(stream != null);
        Debug.Assert(stream.CanRead);

        var readLength = (int)(stream.Length - stream.Position);

        Debug.Assert(readLength <= (stream.Length - stream.Position));

        if (readLength == 0)
        {
            return [];
        }

        var buffer = new byte[readLength];
        var bytesRead = 0;

        while (bytesRead < readLength)
        {
            bytesRead += stream.Read(buffer, bytesRead, readLength - bytesRead);
        }

        return buffer;
    }

    /// <summary>
    /// Computes a hashcode for a dictionary.
    /// </summary>
    /// <returns>Hashcode for the list.</returns>
    public static int GetHashCode(IDictionary dict)
    {
        if (dict is null)
            return 0;

        var hashCode = 0;
        foreach (DictionaryEntry keyValue in dict)
        {
            hashCode = (hashCode * 397) ^ keyValue.Key.GetHashCode();
            hashCode = (hashCode * 397) ^ (keyValue.Value?.GetHashCode() ?? 0);
        }
        return hashCode;
    }

    /// <summary>
    /// Computes a hashcode for an enumeration
    /// </summary>
    /// <param name="it">An enumerator.</param>
    /// <returns>Hashcode for the list.</returns>
    public static int GetHashCode(IEnumerable it)
    {
        if (it is null)
            return 0;

        var hashCode = 0;
        foreach (var current in it)
        {
            hashCode = (hashCode * 397) ^ (current?.GetHashCode() ?? 0);
        }
        return hashCode;
    }

    /// <summary>
    /// Computes a hashcode for an enumeration
    /// </summary>
    /// <param name="it">An enumerator.</param>
    /// <returns>Hashcode for the list.</returns>
    public static int GetHashCode(IEnumerator it)
    {
        if (it is null)
            return 0;

        var hashCode = 0;
        while (it.MoveNext())
        {
            var current = it.Current;
            hashCode = (hashCode * 397) ^ (current?.GetHashCode() ?? 0);
        }
        return hashCode;
    }

    /// <summary>
    /// Compares two collection, element by elements.
    /// </summary>
    /// <param name="first">The collection to compare from.</param>
    /// <param name="second">The colllection to compare to.</param>
    /// <returns>True if lists are identical (but no necessarely of the same time). False otherwise.</returns>
    public static bool Compare<TKey, TValue>(IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
    {
        if (ReferenceEquals(first, second)) return true;
        if (first is null || second is null) return false;
        if (first.Count != second.Count) return false;

        var comparer = EqualityComparer<TValue>.Default;

        foreach (var keyValue in first)
        {
            if (!second.TryGetValue(keyValue.Key, out var secondValue)) return false;
            if (!comparer.Equals(keyValue.Value, secondValue)) return false;
        }

        // Check that all keys in second are in first
        return second.Keys.All(first.ContainsKey);
    }

    /// <summary>
    /// Compares two collection, element by elements.
    /// </summary>
    /// <param name="first">The collection to compare from.</param>
    /// <param name="second">The colllection to compare to.</param>
    /// <returns>True if lists are identical (but not necessarily in the same order). False otherwise.</returns>
    /// <remarks>Concrete SortedList is favored over interface to avoid enumerator object allocation.</remarks>
    public static bool Compare<TKey, TValue>(Collections.SortedList<TKey, TValue> first, Collections.SortedList<TKey, TValue> second)
    {
        if (ReferenceEquals(first, second)) return true;
        if (first is null || second is null) return false;
        if (first.Count != second.Count) return false;

        var comparer = EqualityComparer<TValue>.Default;

        foreach (var keyValue in first)
        {
            if (!second.TryGetValue(keyValue.Key, out var secondValue)) return false;
            if (!comparer.Equals(keyValue.Value, secondValue)) return false;
        }

        return true;
    }

    /// <summary>
    /// Linq assisted full tree iteration and collection in a single line.
    /// Warning, could be slow.
    /// </summary>
    /// <typeparam name="T">The type to iterate.</typeparam>
    /// <param name="root">The root item</param>
    /// <param name="childrenF">The function to retrieve a child</param>
    public static IEnumerable<T> IterateTree<T>(T root, Func<T, IEnumerable<T>> childrenF)
    {
        var q = new List<T> { root };
        while (q.Count != 0)
        {
            var c = q[0];
            q.RemoveAt(0);
            q.AddRange(childrenF(c) ?? []);
            yield return c;
        }
    }

    /// <summary>
    /// Converts a <see cref="Stopwatch" /> raw time to a <see cref="TimeSpan" />.
    /// </summary>
    /// <param name="delta">The delta.</param>
    /// <returns>The <see cref="TimeSpan" />.</returns>
    public static TimeSpan ConvertRawToTimestamp(long delta)
        => delta == 0 ? default : TimeSpan.FromTicks(delta * TimeSpan.TicksPerSecond / Stopwatch.Frequency);
}
