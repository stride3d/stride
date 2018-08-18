// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using Xenko.Core.Annotations;

namespace Xenko.Core.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Tells whether a sequence is null or empty.
        /// </summary>
        /// <param name="source">The source sequence.</param>
        /// <returns>Returns true if the sequence is null or empty, false if it is not null and contains at least one element.</returns>
        [Pure]
        public static bool IsNullOrEmpty(this IEnumerable source)
        {
            if (source == null)
                return true;

            var enumerator = source.GetEnumerator();
            if (enumerator == null)
                throw new ArgumentException("Invalid 'source' IEnumerable.");

            return enumerator.MoveNext() == false;
        }

        /// <summary>
        /// Executes an action for each (casted) item of the given enumerable.
        /// </summary>
        /// <typeparam name="T">Type of the item value in the enumerable.</typeparam>
        /// <param name="source">Input enumerable to work on.</param>
        /// <param name="action">Action performed for each item in the enumerable.</param>
        /// <remarks>This extension method do not yield. It acts just like a foreach statement, and performs a cast to a typed enumerable in the middle.</remarks>
        public static void ForEach<T>([NotNull] this IEnumerable source, [NotNull] Action<T> action)
        {
            source.Cast<T>().ForEach(action);
        }

        /// <summary>
        /// Executes an action for each item of the given enumerable.
        /// </summary>
        /// <typeparam name="T">Type of the item value in the enumerable.</typeparam>
        /// <param name="source">Input enumerable to work on.</param>
        /// <param name="action">Action performed for each item in the enumerable.</param>
        /// <remarks>This extension method do not yield. It acts just like a foreach statement.</remarks>
        public static void ForEach<T>([NotNull] this IEnumerable<T> source, [NotNull] Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);
            }
        }

        /// <summary>
        /// An <see cref="IEnumerable{T}"/> extension method that searches for the first match and returns its index.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="source">Input enumerable to work on.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>The index of the first element matching.</returns>
        [Pure]
        public static int IndexOf<T>([NotNull] this IEnumerable<T> source, [NotNull] Func<T, bool> predicate)
        {
            var index = 0;
            foreach (var item in source)
            {
                if (predicate(item))
                    return index;
                index++;
            }
            return -1;
        }

        /// <summary>
        /// An <see cref="IEnumerable{T}"/> extension method that searches for the last match and returns its index.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="source">Input enumerable to work on.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>The index of the last element matching.</returns>
        [Pure]
        public static int LastIndexOf<T>([NotNull] this IEnumerable<T> source, [NotNull] Func<T, bool> predicate)
        {
            var list = source as IList<T>;
            if (list != null)
            {
                // Faster search for lists.
                for (var i = list.Count - 1; i >= 0; --i)
                {
                    if (predicate(list[i]))
                        return i;
                }
                return -1;
            }
            var index = 0;
            var lastIndex = -1;
            foreach (var item in source)
            {
                if (predicate(item))
                    lastIndex = index;
                index++;
            }
            return lastIndex;
        }

        /// <summary>
        /// Filters out null items from the enumerable.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="source">Input enumerable to work on.</param>
        /// <returns>An enumeration of all items in <paramref name="source"/> that are not <c>null</c>.</returns>
        [ItemNotNull, NotNull, Pure]
        public static IEnumerable<T> NotNull<T>([NotNull] this IEnumerable<T> source) where T : class
        {
            return source.Where(x => x != null);
        }

        /// <summary>
        /// Filters out null items from the enumerable.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="source">Input enumerable to work on.</param>
        /// <returns>An enumeration of all items in <paramref name="source"/> that are not <c>null</c>.</returns>
        [NotNull, Pure]
        public static IEnumerable<T> NotNull<T>([NotNull] this IEnumerable<T?> source) where T : struct
        {
            return source.Where(item => item.HasValue).Select(item => item.Value);
        }

        /// <summary>
        /// Enumerates the linked list nodes.
        /// </summary>
        /// <param name="list">The linked list.</param>
        /// <returns>An enumeration of the linked list nodes.</returns>
        [ItemNotNull, NotNull, Pure]
        internal static IEnumerable<LinkedListNode<T>> EnumerateNodes<T>([NotNull] this LinkedList<T> list)
        {
            var node = list.First;
            while (node != null)
            {
                yield return node;
                node = node.Next;
            }
        }
    }
}
