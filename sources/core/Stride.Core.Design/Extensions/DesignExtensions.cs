// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Core.Annotations;

namespace Stride.Core.Extensions
{
    public static class DesignExtensions
    {
        /// <summary>
        /// Checks whether the IEnumerable represents a readonly data source.
        /// </summary>
        /// <param name="source">The IEnumerable to check.</param>
        /// <returns>Returns true if the data source is readonly, false otherwise.</returns>
        [Pure]
        public static bool IsReadOnly([NoEnumeration, NotNull] this IEnumerable source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var collection = source as ICollection<object>;
            if (collection != null)
                return collection.IsReadOnly;

            var list = source as IList;
            if (list != null)
                return list.IsReadOnly;

            return true;
        }

        /// <summary>
        /// Allow to directly iterate over an enumerator type.
        /// </summary>
        /// <typeparam name="T">Type of items provided by the enumerator.</typeparam>
        /// <param name="enumerator">Enumerator instance to iterate on.</param>
        /// <returns>Returns an enumerable that can be consumed in a foreach statement.</returns>
        [NotNull, Pure]
        public static IEnumerable<T> Enumerate<T>([NotNull] this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        /// <summary>
        /// Allow to directly iterate over an enumerator type.
        /// </summary>
        /// <typeparam name="T">Type of items provided by the enumerator.</typeparam>
        /// <param name="enumerator">Enumerator instance to iterate on. (subtype is casted to T)</param>
        /// <returns>Returns a typed enumerable that can be consumed in a foreach statement.</returns>
        [NotNull, Pure]
        public static IEnumerable<T> Enumerate<T>([NotNull] this IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
                yield return (T)enumerator.Current;
        }

        /// <summary>
        /// Zips two sequences together into a sequence of tuples.
        /// </summary>
        /// <typeparam name="T1">Type of elements in the first sequence.</typeparam>
        /// <typeparam name="T2">Type of elements in the second sequence.</typeparam>
        /// <param name="enumerable1">The first sequence to be zipped.</param>
        /// <param name="enumerable2">The second sequence to be zipped.</param>
        /// <returns>An enumerable sequence of tuples containing elements from both sequences.</returns>
        [NotNull, Pure]
        public static IEnumerable<Tuple<T1, T2>> Zip<T1, T2>([NotNull] this IEnumerable<T1> enumerable1, [NotNull] IEnumerable<T2> enumerable2)
        {
            if (enumerable1 == null) throw new ArgumentNullException(nameof(enumerable1));
            if (enumerable2 == null) throw new ArgumentNullException(nameof(enumerable2));

            using (var enumerator1 = enumerable1.GetEnumerator())
            {
                using (var enumerator2 = enumerable2.GetEnumerator())
                {
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
        }

        /// <summary>
        /// Iterates over all elements of source and their children recursively.
        /// </summary>
        /// <typeparam name="T">The type of elements.</typeparam>
        /// <param name="source">The source enumeration.</param>
        /// <param name="childrenSelector">A function that returns the children of an element.</param>
        /// <returns>An enumeration of all elements of source and their children in depth-first order.</returns>
        [NotNull, Pure]
        public static IEnumerable<T> SelectDeep<T>(this IEnumerable<T> source, [NotNull] Func<T, IEnumerable<T>> childrenSelector)
        {
            if (childrenSelector == null) throw new ArgumentNullException(nameof(childrenSelector));

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

        /// <summary>
        /// Iterates over all elements of source and their children in breadth-first order.
        /// </summary>
        /// <typeparam name="T">Type of the elements.</typeparam>
        /// <param name="source">The root enumeration.</param>
        /// <param name="childrenSelector">A function that returns the children of an element.</param>
        /// <returns>An enumeration of all elements of source and their children in breadth-first order.</returns>
        [NotNull, Pure]
        public static IEnumerable<T> BreadthFirst<T>(this IEnumerable<T> source, [NotNull] Func<T, IEnumerable<T>> childrenSelector)
        {
            if (childrenSelector == null) throw new ArgumentNullException(nameof(childrenSelector));

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

        /// <summary>
        /// Iterates over all elements of source and their children in depth-first order.
        /// </summary>
        /// <typeparam name="T">Type of the elements.</typeparam>
        /// <param name="source">The root enumeration.</param>
        /// <param name="childrenSelector">A function that returns the children of an element.</param>
        /// <returns>An enumeration of all elements of source and their children in depth-first order.</returns>
        [NotNull, Pure]
        public static IEnumerable<T> DepthFirst<T>([NotNull] this IEnumerable<T> source, [NotNull] Func<T, IEnumerable<T>> childrenSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (childrenSelector == null) throw new ArgumentNullException(nameof(childrenSelector));

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

        /// <summary>
        /// Returns distinct elements from a sequence using a specified key selector.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key used for comparison.</typeparam>
        /// <param name="source">The sequence from which to retrieve distinct elements.</param>
        /// <param name="selector">A function to extract a key from each element.</param>
        /// <returns>An enumeration of distinct elements from the source sequence based on the selected key.</returns>
        [NotNull, Pure]
        public static IEnumerable<T> Distinct<T, TKey>([NotNull] this IEnumerable<T> source, [NotNull] Func<T, TKey> selector)
        {
            return source.Distinct(new SelectorEqualityComparer<T, TKey>(selector));
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing their elements using the default equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequences.</typeparam>
        /// <param name="a1">The first sequence to compare.</param>
        /// <param name="a2">The second sequence to compare.</param>
        /// <param name="comparer">An optional equality comparer for elements.</param>
        /// <returns>True if the sequences are equal, otherwise false.</returns>
        [Pure]
        public static bool Equals<T>(IEnumerable<T> a1, IEnumerable<T> a2, IEqualityComparer<T> comparer = null)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            using (var e1 = a1.GetEnumerator())
            using (var e2 = a2.GetEnumerator())
            {
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
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing their elements using the default equality comparer.
        /// </summary>
        /// <param name="a1">The first sequence to compare.</param>
        /// <param name="a2">The second sequence to compare.</param>
        /// <returns>True if the sequences are equal, otherwise false.</returns>
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
                if (e1.Current != null && !e1.Current.Equals(e2.Current))
                    return false;

                // item from the second enum is non null and does not equal item from the first enum
                if (e2.Current != null && !e2.Current.Equals(e1.Current))
                    return false;
            }
        }

        /// <summary>
        /// Determines whether all elements in a sequence are equal.
        /// </summary>
        /// <param name="values">The sequence of values to compare.</param>
        /// <param name="value">The common value if all elements are equal; otherwise, null.</param>
        /// <returns>True if all elements are equal; otherwise, false.</returns>
        [Pure]
        public static bool AllEqual([NotNull] this IEnumerable<object> values, [CanBeNull] out object value)
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
        public static TValue GetOrCreateValue<TKey, TValue>([NotNull] this IDictionary<TKey, TValue> dictionary, [NotNull] TKey key)
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
        public static TValue GetOrCreateValue<TKey, TValue>([NotNull] this IDictionary<TKey, TValue> dictionary, [NotNull] TKey key, [NotNull] Func<TKey, TValue> createValueFunc)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = createValueFunc.Invoke(key);
                dictionary.Add(key, value);
            }
            return value;
        }

        /// <summary>
        /// Removes elements from the list based on a predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to remove elements from.</param>
        /// <param name="predicate">A predicate function that determines which elements to remove.</param>
        /// <returns>The number of elements removed from the list.</returns>
        public static int RemoveWhere<T>([NotNull] this IList<T> list, [NotNull] Predicate<T> predicate)
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

        /// <summary>
        /// Moves items in the list that match a predicate to a new index.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to move items within.</param>
        /// <param name="predicate">A predicate function that determines which items to move.</param>
        /// <param name="newIndex">The new index to move the matching items to.</param>
        /// <returns>The number of items moved.</returns>
        public static int MoveMatchingItemToIndex<T>([NotNull] this IList<T> list, [NotNull] Predicate<T> predicate, int newIndex)
        {
            var count = 0;
            var index = newIndex;

            for (var i = 0; i < list.Count; ++i)
            {
                if (predicate(list[i]))
                {
                    if (index != i)
                    {
                        var itemToMove = list[i];
                        list.RemoveAt(i);
                        list.Insert(index, itemToMove);
                    }
                    ++index;
                    ++count;
                }
            }

            return count;
        }

        /// <summary>
        /// Removes elements from the collection based on a predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="collection">The collection to remove elements from.</param>
        /// <param name="predicate">A predicate function that determines which elements to remove.</param>
        /// <returns>The number of elements removed from the collection.</returns>
        public static int RemoveWhere<T>([NotNull] this ICollection<T> collection, [NotNull] Predicate<T> predicate)
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

            public SelectorEqualityComparer([NotNull] Func<T, TKey> selector)
            {
                this.selector = selector;
            }

            public bool Equals(T x, T y)
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
}
