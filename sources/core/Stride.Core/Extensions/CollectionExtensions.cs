// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Annotations;

namespace Stride.Core.Extensions
{
    /// <summary>
    /// An extension class for various types of collection.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Remove an item by swapping it with the last item and removing it from the last position. This function prevents to shift values from the list on removal but does not maintain order.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="item">The item to remove.</param>
        public static void SwapRemove<T>([NotNull] this IList<T> list, T item)
        {
            var index = list.IndexOf(item);
            if (index < 0)
                return;

            list.SwapRemoveAt(index);
        }

        /// <summary>
        /// Remove an item by swapping it with the last item and removing it from the last position. This function prevents to shift values from the list on removal but does not maintain order.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="index">Index of the item to remove.</param>
        public static void SwapRemoveAt<T>([NotNull] this IList<T> list, int index)
        {
            if (index < 0 || index >= list.Count) throw new ArgumentOutOfRangeException(nameof(index));

            if (index < list.Count - 1)
            {
                list[index] = list[list.Count - 1];
            }

            list.RemoveAt(list.Count - 1);
        }

        /// <summary>
        /// Gets the item from a list at a specified index. If index is out of the list, returns null.
        /// </summary>
        /// <typeparam name="T">Type of the item in the list</typeparam>
        /// <param name="list">The list.</param>
        /// <param name="index">The index.</param>
        /// <returns>The item from a list at a specified index. If index is out of the list, returns null..</returns>
        public static T GetItemOrNull<T>([NotNull] this IList<T> list, int index) where T : class
        {
            if (index >= 0 && index < list.Count)
            {
                return list[index];
            }
            return null;
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="IReadOnlyList{T}"/>.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="item">The object to locate in the <see cref="IReadOnlyList{T}"/>.</param>
        /// <returns>The index of item if found in the list; otherwise, -1.</returns>
        public static int IndexOf<T>([NotNull] this IReadOnlyList<T> list, T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < list.Count; i++)
            {
                if (comparer.Equals(list[i], item)) return i;
            }
            return -1;
        }
    }
}
