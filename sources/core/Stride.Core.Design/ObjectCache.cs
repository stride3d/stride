// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;

namespace Stride.Core
{
    /// <summary>
    /// A thread-safe cache of indexed objects that will keep the most recently accessed instance around in a fixed size buffer.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used to index objects.</typeparam>
    /// <typeparam name="TValue">The type of objects contained in the cache.</typeparam>
    public class ObjectCache<TKey, TValue> where TKey : IEquatable<TKey> where TValue : class
    {
        /// <summary>
        /// The default size of the cache when an instance of <see cref="ObjectCache{TKey, TValue}"/> is created with the parameterless constructor.
        /// </summary>
        public const int DefaultCacheSize = 256;

        private readonly SortedList<long, TKey> accessHistory;
        private readonly Dictionary<TKey, TValue> cache;
        private readonly object objectLock = new object();
        private int size;
        private long currentAccessCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectCache{TKey, TValue}"/> class with a custom comparer for the keys.
        /// </summary>
        /// <param name="size">The size of this cache.</param>
        /// <param name="comparer">The comparer to use to compare keys.</param>
        public ObjectCache(int size, IEqualityComparer<TKey> comparer)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            this.size = size;
            cache = new Dictionary<TKey, TValue>(size, comparer);
            accessHistory = new SortedList<long, TKey>(size);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectCache{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="size">The size of this cache.</param>
        public ObjectCache(int size)
            : this(size, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectCache{TKey, TValue}"/> class with a default size equal to <see cref="DefaultCacheSize"/>.
        /// </summary>
        public ObjectCache()
            : this(256)
        {
        }

        /// <summary>
        /// Gets or sets the size of the cache.
        /// </summary>
        /// <remarks>If the new value is smaller than the current size, oldest accessed items will be removed from the cache immediately.</remarks>
        public int Size { get { return size; } set { Resize(value); } }

        /// <summary>
        /// Tries to retrieve the object corresponding to the given key in the cache.
        /// </summary>
        /// <param name="key">The key of the item to look for.</param>
        /// <returns>The object corresponding to the given key if it's available in the cache, <c>Null</c> otherwise.</returns>
        [CanBeNull]
        public TValue TryGet([NotNull] TKey key)
        {
            lock (objectLock)
            {
                TValue result;
                if (cache.TryGetValue(key, out result))
                {
                    // If we were able to find the object, we must update the access history.
                    var historyEntry = accessHistory.FirstOrDefault(x => x.Value.Equals(key)).Key;
                    if (historyEntry != default(long))
                    {
                        // Remove the key if it was already in the history.
                        accessHistory.Remove(historyEntry);
                    }
                    // Insert the key at the end of the access history
                    accessHistory.Add(++currentAccessCount, key);
                }
                return result;
            }
        }

        /// <summary>
        /// Adds the given indexed object in the cache.
        /// </summary>
        /// <param name="key">The key of the object.</param>
        /// <param name="value">The object to cache.</param>
        public void Cache([NotNull] TKey key, TValue value)
        {
            lock (objectLock)
            {
                // If the cache is full, remove the oldest accessed items, and reserve space for the item we want to add.
                ShrinkCache(1);
                // Add the object to the cache
                cache.Add(key, value);
                // Make an access on it to set it as the most recently accessed object.
                TryGet(key);
            }
        }

        private void Resize(int newSize)
        {
            lock (objectLock)
            {
                if (newSize <= 0) throw new ArgumentOutOfRangeException(nameof(size), @"The size must be a positive non-null integer.");
                size = newSize;
                // Remove any object that is beyond our new size.
                ShrinkCache();
            }
        }

        private void ShrinkCache(int emptySlotToKeep = 0)
        {
            while (cache.Count + emptySlotToKeep > Size)
            {
                var keyToRemove = accessHistory.First();
                accessHistory.RemoveAt(0);
                cache.Remove(keyToRemove.Value);
            }
        }
    }
}
