// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Core.Annotations;

namespace Stride.Core.Extensions;

public static class DesignExtensions
{
    /// <summary>
    /// Checks whether the IEnumerable represents a readonly data source.
    /// </summary>
    /// <param name="source">The IEnumerable to check.</param>
    /// <returns>Returns true if the data source is readonly, false otherwise.</returns>
    [Pure]
    public static bool IsReadOnly([NoEnumeration] this IEnumerable source)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(source);
#else
        if (source is null) throw new ArgumentNullException(nameof(source));
#endif

        if (source is ICollection<object> collection)
            return collection.IsReadOnly;

        if (source is IList list)
            return list.IsReadOnly;

        return true;
    }

    /// <summary>
    /// Allow to directly iterate over an enumerator type.
    /// </summary>
    /// <typeparam name="T">Type of items provided by the enumerator.</typeparam>
    /// <param name="enumerator">Enumerator instance to iterate on.</param>
    /// <returns>Returns an enumerable that can be consume in a foreach statement.</returns>
    [Pure]
    public static IEnumerable<T> Enumerate<T>(this IEnumerator<T> enumerator)
    {
        while (enumerator.MoveNext())
            yield return enumerator.Current;
    }

    /// <summary>
    /// Allow to directly iterate over an enumerator type.
    /// </summary>
    /// <typeparam name="T">Type of items provided by the enumerator.</typeparam>
    /// <param name="enumerator">Enumerator instance to iterate on. (subtype is casted to T)</param>
    /// <returns>Returns a typed enumerable that can be consume in a foreach statement.</returns>
    [Pure]
    public static IEnumerable<T> Enumerate<T>(this IEnumerator enumerator)
    {
        while (enumerator.MoveNext())
            yield return (T)enumerator.Current;
    }

    [Pure]
    public static IEnumerable<Tuple<T1, T2>> Zip<T1, T2>(this IEnumerable<T1> enumerable1, IEnumerable<T2> enumerable2)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(enumerable1);
        ArgumentNullException.ThrowIfNull(enumerable2);
#else
        if (enumerable1 is null) throw new ArgumentNullException(nameof(enumerable1));
        if (enumerable2 is null) throw new ArgumentNullException(nameof(enumerable2));
#endif

        return Impl();

        IEnumerable<Tuple<T1, T2>> Impl()
        {
            using var enumerator1 = enumerable1.GetEnumerator();
            using var enumerator2 = enumerable2.GetEnumerator();
            var enumMoved = true;
            while (enumMoved)
            {
                enumMoved = enumerator1.MoveNext();
                var enum2Moved = enumerator2.MoveNext();
                if (enumMoved != enum2Moved)
                    throw new InvalidOperationException("Enumerables do not have the same number of items.");

                if (enumMoved)
                {
                    yield return Tuple.Create(enumerator1.Current, enumerator2.Current);
                }
            }
        }
    }

    /// <summary>
    /// Iterates over all elements of source and their children recursively.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="childrenSelector">The children selector.</param>
    /// <returns></returns>
    [Pure]
    public static IEnumerable<T> SelectDeep<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> childrenSelector)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(childrenSelector);
#else
        if (childrenSelector is null) throw new ArgumentNullException(nameof(childrenSelector));
#endif

        return Impl();

        IEnumerable<T> Impl()
        {
            var stack = new Stack<IEnumerable<T>>();
            stack.Push(source);
            while (stack.Count != 0)
            {
                var current = stack.Pop();
                if (current == null)
                    continue;

                foreach (var item in current)
                {
                    yield return item;
                    stack.Push(childrenSelector(item));
                }
            }
        }
    }

    /// <summary>
    /// Iterates over all elements of source and their children in breadth-first order.
    /// </summary>
    /// <typeparam name="T">Type of the elements.</typeparam>
    /// <param name="source">The root enumeration.</param>
    /// <param name="childrenSelector">A function that returns the children of an element.</param>
    /// <returns>An enumeration of all elements of source and their children in breadth-first order.</returns>
    [Pure]
    public static IEnumerable<T> BreadthFirst<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> childrenSelector)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(childrenSelector);
#else
        if (childrenSelector is null) throw new ArgumentNullException(nameof(childrenSelector));
#endif

        return Impl();

        IEnumerable<T> Impl()
        {
            var queue = new Queue<IEnumerable<T>>();
            queue.Enqueue(source);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == null)
                    continue;

                foreach (var item in current)
                {
                    yield return item;
                    queue.Enqueue(childrenSelector(item));
                }
            }
        }
    }

    /// <summary>
    /// Iterates over all elements of source and their children in depth-first order.
    /// </summary>
    /// <typeparam name="T">Type of the elements.</typeparam>
    /// <param name="source">The root enumeration.</param>
    /// <param name="childrenSelector">A function that returns the children of an element.</param>
    /// <returns>An enumeration of all elements of source and their children in depth-first order.</returns>
    [Pure]
    public static IEnumerable<T> DepthFirst<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> childrenSelector)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(childrenSelector);
#else
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (childrenSelector is null) throw new ArgumentNullException(nameof(childrenSelector));
#endif

        return Impl();

        IEnumerable<T> Impl()
        {
            var nodes = new Stack<T>();
            foreach (var item in source)
            {
                if (item == null)
                    continue;

                nodes.Push(item);
                while (nodes.Count > 0)
                {
                    var node = nodes.Pop();
                    yield return node;
                    foreach (var n in childrenSelector(node).Reverse()) nodes.Push(n);
                }
            }
        }
    }

    [Pure]
    public static IEnumerable<T> Distinct<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector)
    {
        return source.Distinct(new SelectorEqualityComparer<T, TKey>(selector));
    }

    [Pure]
    public static bool Equals<T>(IEnumerable<T> a1, IEnumerable<T> a2, IEqualityComparer<T>? comparer = null)
    {
        if (ReferenceEquals(a1, a2))
            return true;

        if (a1 == null || a2 == null)
            return false;

        comparer ??= EqualityComparer<T>.Default;

        using var e1 = a1.GetEnumerator();
        using var e2 = a2.GetEnumerator();
        while (true)
        {
            var move1 = e1.MoveNext();
            var move2 = e2.MoveNext();

            // End of enumeration, success!
            if (!move1 && !move2)
                break;

            // One of the IEnumerable is shorter than the other?
            if (move1 ^ move2)
                return false;

            if (!comparer.Equals(e1.Current, e2.Current))
                return false;
        }
        return true;
    }

    [Pure]
    public static bool SequenceEqual(this IEnumerable a1, IEnumerable a2)
    {
        if (ReferenceEquals(a1, a2))
            return true;

        if (a1 == null || a2 == null)
            return false;

        var e1 = a1.GetEnumerator();
        var e2 = a2.GetEnumerator();

        while (true)
        {
            var move1 = e1.MoveNext();
            var move2 = e2.MoveNext();

            // End of enumeration, success!
            if (!move1 && !move2)
                return true;

            // One of the IEnumerable is shorter than the other?
            if (move1 ^ move2)
                return false;

            // item from the first enum is non null and does not equal item from the second enum
            if (e1.Current?.Equals(e2.Current) == false)
                return false;

            // item from the second enum is non null and does not equal item from the first enum
            if (e2.Current?.Equals(e1.Current) == false)
                return false;
        }
    }

    [Pure]
    public static bool AllEqual(this IEnumerable<object?> values, out object? value)
    {
        value = null;
        var firstNotNull = values.FirstOrDefault(x => x != null);
        // Either empty, or everything is null
        if (firstNotNull == null)
            return true;

        value = firstNotNull;

        return values.SkipWhile(x => x != firstNotNull).All(firstNotNull.Equals);
    }

    /// <summary>
    /// Returns the value corresponding to the given key.
    /// If the key is absent from the dictionary, it is added with a new instance of the <typeparamref name="TValue"/> type.
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key of the value we are looking for.</param>
    /// <returns>The value attached to key, if key already exists in the dictionary; otherwise, a new instance of the <typeparamref name="TValue"/> type.</returns>
    /// <seealso cref="GetOrCreateValue{TKey,TValue}(IDictionary{TKey,TValue},TKey,Func{TKey, TValue})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue GetOrCreateValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : new()
    {
        return GetOrCreateValue(dictionary, key, _ => new TValue());
    }

    /// <summary>
    /// Returns the value corresponding to the given key.
    /// If the key is absent from the dictionary, the method invokes a callback function to create a value that is bound to the specified key.
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key of the value we are looking for.</param>
    /// <param name="createValueFunc">A function that can create a value for the given key. It has a single parameter of type <typeparamref name="TKey"/>, and returns a value of type <typeparamref name="TValue"/>.</param>
    /// <returns>The value attached to key, if key already exists in the dictionary; otherwise, the new value returned by the <paramref name="createValueFunc"/>.</returns>
    public static TValue GetOrCreateValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> createValueFunc)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(dictionary);
#else
        if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));
#endif
        if (!dictionary.TryGetValue(key, out var value))
        {
            value = createValueFunc.Invoke(key);
            dictionary.Add(key, value);
        }
        return value;
    }

    public static int RemoveWhere<T>(this IList<T> list, Predicate<T> predicate)
    {
        var count = 0;
        var array = list.ToArray();
        for (var i = array.Length - 1; i >= 0; --i)
        {
            if (predicate(array[i]))
                list.RemoveAt(i);

            ++count;
        }
        return count;
    }

    public static int RemoveWhere<T>(this ICollection<T> collection, Predicate<T> predicate)
    {
        var count = 0;
        foreach (var item in collection.ToArray().Where(x => predicate(x)))
        {
            collection.Remove(item);
            ++count;
        }
        return count;
    }

    private class SelectorEqualityComparer<T, TKey> : IEqualityComparer<T>
    {
        private readonly Func<T, TKey> selector;

        public SelectorEqualityComparer(Func<T, TKey> selector)
        {
            this.selector = selector;
        }

        public bool Equals(T? x, T? y)
        {
            var keyX = selector(x);
            var keyY = selector(y);
            if (!typeof(T).GetTypeInfo().IsValueType)
            {
                if (ReferenceEquals(keyX, null))
                    return ReferenceEquals(keyY, null);
            }

            return selector(x).Equals(selector(y));
        }

        public int GetHashCode(T obj)
        {
            var key = selector(obj);
            if (!typeof(T).GetTypeInfo().IsValueType && ReferenceEquals(key, null))
                return 0;
            return key.GetHashCode();
        }
    }
}
