// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Stride.Core.IO
{
    /// <summary>
    /// A Key->Value store that will be incrementally saved on the HDD.
    /// Thread-safe and process-safe.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class DictionaryStore<TKey, TValue> : Store<KeyValuePair<TKey, TValue>>
    {
        protected readonly Dictionary<TKey, TValue> loadedIdMap = new Dictionary<TKey, TValue>();
        protected readonly Dictionary<TKey, UnsavedIdMapEntry> unsavedIdMap = new Dictionary<TKey, UnsavedIdMapEntry>();

        public DictionaryStore(Stream stream) : base(stream)
        {
        }

        /// <summary>
        /// Gets the values stored including unsaved.
        /// </summary>
        /// <returns>Values stored including unsaved.</returns>
        public KeyValuePair<TKey, TValue>[] GetValues()
        {
            lock (lockObject)
            {
                var result = new KeyValuePair<TKey, TValue>[loadedIdMap.Count + unsavedIdMap.Count];
                int i = 0;
                foreach (var value in loadedIdMap)
                {
                    result[i++] = value;
                }
                foreach (var item in unsavedIdMap)
                {
                    result[i++] = new KeyValuePair<TKey, TValue>(item.Key, item.Value.Value);
                }

                return result;
            }
        }

        /// <summary>
        /// Gets or sets the item with the specified key.
        /// </summary>
        /// <value>
        /// The item to get or set.
        /// </value>
        /// <param name="key">The key of the item to get or set.</param>
        /// <returns></returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public bool Contains(TKey key)
        {
            TValue value;
            return TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets or sets the item with the specified key.
        /// </summary>
        /// <value>
        /// The item to get or set.
        /// </value>
        /// <param name="key">The key of the item to get or set.</param>
        /// <returns></returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (!TryGetValue(key, out value))
                    throw new KeyNotFoundException();
                return value;
            }
            set
            {
                AddValue(new KeyValuePair<TKey, TValue>(key, value));
            }
        }

        /// <summary>
        /// Tries to get the value from its key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (lockObject)
            {
                UnsavedIdMapEntry unsavedIdMapEntry;
                if (unsavedIdMap.TryGetValue(key, out unsavedIdMapEntry))
                {
                    value = unsavedIdMapEntry.Value;
                    return true;
                }

                return loadedIdMap.TryGetValue(key, out value);
            }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> SearchValues(Func<KeyValuePair<TKey, TValue>, bool> predicate)
        {
            lock (lockObject)
            {
                var result = new Dictionary<TKey, TValue>(loadedIdMap.Count + unsavedIdMap.Count);

                foreach (var item in loadedIdMap)
                {
                    if (predicate(new KeyValuePair<TKey, TValue>(item.Key, item.Value)))
                    {
                        result[item.Key] = item.Value;
                    }
                }

                foreach (var item in unsavedIdMap)
                {
                    if (predicate(new KeyValuePair<TKey, TValue>(item.Key, item.Value.Value)))
                    {
                        result[item.Key] = item.Value.Value;
                    }
                }

                return result;
            }
        }

        protected override void AddUnsaved(KeyValuePair<TKey, TValue> item, int currentTransaction)
        {
            var unsavedIdMapEntry = new UnsavedIdMapEntry { Value = item.Value, Transaction = currentTransaction };
            unsavedIdMap[item.Key] = unsavedIdMapEntry;
        }

        protected override void RemoveUnsaved(KeyValuePair<TKey, TValue> item, int currentTransaction)
        {
            UnsavedIdMapEntry entry;
            if (unsavedIdMap.TryGetValue(item.Key, out entry))
            {
                if (entry.Transaction == currentTransaction)
                {
                    unsavedIdMap.Remove(item.Key);
                }
            }
        }

        protected override void AddLoaded(KeyValuePair<TKey, TValue> item)
        {
            loadedIdMap[item.Key] = item.Value;
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> GetPendingItems(int currentTransaction)
        {
            var transactionIds = new List<KeyValuePair<TKey, TValue>>();

            foreach (var unsavedIdMapEntry in unsavedIdMap.Where(x => x.Value.Transaction == currentTransaction))
            {
                transactionIds.Add(new KeyValuePair<TKey, TValue>(unsavedIdMapEntry.Key, unsavedIdMapEntry.Value.Value));
            }

            return transactionIds.ToArray();
        }

        protected override void ResetInternal()
        {
            loadedIdMap.Clear();
            unsavedIdMap.Clear();
        }

        protected struct UnsavedIdMapEntry
        {
            public int Transaction;
            public TValue Value;
        }
    }
}
