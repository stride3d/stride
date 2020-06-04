// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// A container for item identifiers and similar metadata that is associated to a collection or a dictionary.
    /// </summary>
    // TODO: Arrange the API of this class once all use cases have been implemented
    public class CollectionItemIdentifiers : IEnumerable<KeyValuePair<object, ItemId>>
    {
        private readonly Dictionary<object, ItemId> keyToIdMap = new Dictionary<object, ItemId>();

        private readonly HashSet<ItemId> deletedItems = new HashSet<ItemId>();

        /// <summary>
        /// Gets or sets the <see cref="ItemId"/> corresponding to the given key.
        /// </summary>
        /// <param name="key">The key for which to retrieve the <see cref="ItemId"/>.</param>
        /// <returns>The <see cref="ItemId"/> corresponding to the given key.</returns>
        public ItemId this[object key] { get { return keyToIdMap[key]; } set { Set(key, value); } }

        /// <summary>
        /// Gets the list of <see cref="ItemId"/> corresponding to deleted items that are being kept in this <see cref="CollectionItemIdentifiers"/>.
        /// </summary>
        public IEnumerable<ItemId> DeletedItems => deletedItems;

        /// <summary>
        /// Gets the number of keys/identifiers in this <see cref="CollectionItemIdentifiers"/>.
        /// </summary>
        public int KeyCount => keyToIdMap.Count;

        /// <summary>
        /// Gets the number of deleted identifiers that are being kept in this <see cref="CollectionItemIdentifiers"/>.
        /// </summary>
        public int DeletedCount => deletedItems.Count;

        public void Add(object key, ItemId id)
        {
            keyToIdMap.Add(key, id);
            if (deletedItems.Contains(id))
                UnmarkAsDeleted(id);
        }

        public void Set(object key, ItemId id)
        {
            keyToIdMap[key] = id;
            if (deletedItems.Contains(id))
                UnmarkAsDeleted(id);
        }

        public void Insert(int index, ItemId id)
        {
            for (var i = keyToIdMap.Count; i > index; --i)
            {
                keyToIdMap[i] = keyToIdMap[i-1];

            }
            keyToIdMap[index] = id;
            if (deletedItems.Contains(id))
                UnmarkAsDeleted(id);
        }

        public void Clear()
        {
            keyToIdMap.Clear();
            deletedItems.Clear();
        }

        public bool ContainsKey(object key)
        {
            return keyToIdMap.ContainsKey(key);
        }

        public bool TryGet(object key, out ItemId id)
        {
            return keyToIdMap.TryGetValue(key, out id);
        }

        public ItemId Delete(object key, bool markAsDeleted = true)
        {
            var id = keyToIdMap[key];
            keyToIdMap.Remove(key);
            if (markAsDeleted)
            {
                MarkAsDeleted(id);
            }
            return id;
        }

        public ItemId DeleteAndShift(int index, bool markAsDeleted = true)
        {
            var id = keyToIdMap[index];
            for (var i = index + 1; i < keyToIdMap.Count; ++i)
            {
                keyToIdMap[i - 1] = keyToIdMap[i];
            }
            keyToIdMap.Remove(keyToIdMap.Count - 1);

            if (markAsDeleted)
            {
                MarkAsDeleted(id);
            }
            return id;
        }

        public void MarkAsDeleted(ItemId id)
        {
            deletedItems.Add(id);
        }

        public void UnmarkAsDeleted(ItemId id)
        {
            deletedItems.Remove(id);
        }

        public void Validate(bool isList)
        {
            var ids = new HashSet<ItemId>(keyToIdMap.Values);
            if (ids.Count != keyToIdMap.Count)
                throw new InvalidOperationException("Two elements of the collection have the same id");

            foreach (var deleted in deletedItems)
                ids.Add(deleted);

            if (ids.Count != keyToIdMap.Count + deletedItems.Count)
                throw new InvalidOperationException("An id is both marked as deleted and associated to a key of the collection.");
        }

        public object GetKey(ItemId itemId)
        {
            object output = null;
            // TODO: add indexing by guid to avoid O(n)
            foreach (var kvp in keyToIdMap)
            {
                if (kvp.Value == itemId)
                {
                    if(output != null)
                        throw new InvalidOperationException("Two elements of the collection have the same id");
                    output = kvp.Key;
                }
            }
            return output;
        }

        public void CloneInto(CollectionItemIdentifiers target, IReadOnlyDictionary<object, object> referenceTypeClonedKeys)
        {
            target.keyToIdMap.Clear();
            target.deletedItems.Clear();
            foreach (var key in keyToIdMap)
            {
                object clonedKey;
                if (key.Key.GetType().IsValueType || referenceTypeClonedKeys == null)
                {
                    target.Add(key.Key, key.Value);
                }
                else if (referenceTypeClonedKeys.TryGetValue(key.Key, out clonedKey))
                {
                    target.Add(clonedKey, key.Value);
                }
                else
                {
                    throw new KeyNotFoundException("Unable to find the non-value type key in the dictionary of cloned keys.");
                }
            }
            foreach (var deletedItem in DeletedItems)
            {
                target.MarkAsDeleted(deletedItem);
            }
        }

        public bool IsDeleted(ItemId itemId)
        {
            return DeletedItems.Contains(itemId);
        }

        public IEnumerator<KeyValuePair<object, ItemId>> GetEnumerator() => keyToIdMap.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
