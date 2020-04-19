// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;

namespace Stride.Core.IO
{
    /// <summary>
    /// A value store that will be incrementally saved on HDD.
    /// Thread-safe and process-safe.
    /// </summary>
    /// <typeparam name="T">The type of elements in the store.</typeparam>
    public class ListStore<T> : Store<T> where T : new()
    {
        protected readonly List<T> loadedIdMap = new List<T>();
        protected readonly LinkedList<UnsavedEntry> unsavedIdMap = new LinkedList<UnsavedEntry>();

        private IEqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;

        public ListStore(Stream stream) : base(stream)
        {
        }

        public T[] GetValues()
        {
            lock (lockObject)
            {
                var result = new T[loadedIdMap.Count + unsavedIdMap.Count];
                int i;
                for (i = 0; i < loadedIdMap.Count; ++i)
                {
                    result[i] = loadedIdMap[i];
                }
                foreach (var item in unsavedIdMap)
                {
                    result[i++] = item.Value;
                }

                return result;
            }
        }

        protected override void AddUnsaved(T item, int currentTransaction)
        {
            unsavedIdMap.AddLast(new UnsavedEntry { Value = item, Transaction = currentTransaction });
        }

        protected override void RemoveUnsaved(T item, int currentTransaction)
        {
            RemoveUnsaved(null, currentTransaction);
        }

        protected override void RemoveUnsaved(IEnumerable<T> items, int currentTransaction)
        {
            var node = unsavedIdMap.First;

            while (node != null)
            {
                var next = node.Next;
                var nodeTransaction = node.Value.Transaction;
                if (nodeTransaction == currentTransaction)
                    unsavedIdMap.Remove(node);
                else if (nodeTransaction > currentTransaction)
                    break; // No need to test further since transaction are ordered
                node = next;
            }
        }

        protected override void AddLoaded(T item)
        {
            loadedIdMap.Add(item);
        }

        protected override IEnumerable<T> GetPendingItems(int currentTransaction)
        {
            var transactionIds = new List<T>();

            foreach (var item in unsavedIdMap)
            {
                if (item.Transaction < currentTransaction)
                    continue;
                if (item.Transaction > currentTransaction)
                    break; // No need to test further since transaction are ordered

                transactionIds.Add(item.Value);
            }

            return transactionIds;
        }

        protected class UnsavedEntry
        {
            public int Transaction;
            public T Value;
        }
    }
}
