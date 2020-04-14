// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Stride.Core.Annotations;

namespace Stride.Core.VisualStudio
{
    public static class KeyedCollectionExtensions
    {
        /// <summary>
        /// Adds the specified enumeration of values to this collection.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="collection">The collection to add the value to.</param>
        /// <param name="items">The items to add to the collection.</param>
        public static void AddRange<TKey, TValue>([NotNull] this KeyedCollection<TKey, TValue> collection, IEnumerable<TValue> items)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    collection.Add(item);
                }
            }
        }
    }
}
