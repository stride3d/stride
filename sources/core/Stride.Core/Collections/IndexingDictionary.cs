// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Serializers;

namespace Xenko.Core.Collections
{
    /// <summary>
    /// A dictionary that maps index values to items. It uses a sparse list internally for storage.
    /// </summary>
    /// <typeparam name="T">The type of item indexed in this collection.</typeparam>
    // TODO: unit test this collection!
    [DataSerializer(typeof(IndexingDictionarySerializer<>), Mode = DataSerializerGenericMode.GenericArguments)]
    public class IndexingDictionary<T> : IDictionary<int, T> where T : class
    {
        private readonly FastList<T> items = new FastList<T>(0);
        private List<int> keys;
        private List<T> values;

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        public T this[int index] { get { return SafeGet(index); } set { SafeSet(index, value); } }

        public ICollection<int> Keys
        {
            get
            {
                if (keys == null)
                {
                    keys = new List<int>(items.Count);
                    for (var i = 0; i < items.Count; ++i)
                    {
                        if (items[i] != null)
                            keys.Add(i);
                    }
                }
                return keys;
            }
        }

        public ICollection<T> Values
        {
            get
            {
                if (values == null)
                {
                    values = new List<T>(items.Count);
                    foreach (var item in items)
                    {
                        if (item != null)
                            values.Add(item);
                    }
                }
                return values;
            }
        }

        public IEnumerator<KeyValuePair<int, T>> GetEnumerator()
        {
            return new Enumerator(items.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<int, T> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            items.Clear();
            keys = null;
            values = null;
            Count = 0;
        }

        public bool Contains(KeyValuePair<int, T> item)
        {
            return SafeGet(item.Key) == item.Value;
        }

        public void CopyTo(KeyValuePair<int, T>[] array, int arrayIndex)
        {
            var index = 0;
            foreach (var item in items)
            {
                if (item != null)
                {
                    array[arrayIndex] = new KeyValuePair<int, T>(index, item);
                    ++arrayIndex;
                }
                ++index;
            }
        }

        public bool Remove(KeyValuePair<int, T> item)
        {
            return SafeGet(item.Key) == item.Value && Remove(item.Key);
        }

        public bool ContainsKey(int index)
        {
            return SafeGet(index) != null;
        }

        public void Add(int key, T value)
        {
            if (SafeGet(key) != null) throw new ArgumentException("An element with the same key has already been added.");
            SafeSet(key, value);
        }

        public bool Remove(int index)
        {
            if (index < 0 || index >= items.Count)
                return false;

            if (items[index] == null)
                return false;

            items[index] = null;
            --Count;
            index = items.Count - 1;
            while (index >= 0 && items[index] == null)
            {
                items.RemoveAt(index);
                --index;
            }
            keys = null;
            values = null;
            return true;
        }

        public bool TryGetValue(int key, out T value)
        {
            value = SafeGet(key);
            return value != null;
        }

        public T SafeGet(int index)
        {
            return index >= 0 && index < items.Count ? items[index] : null;
        }

        private void SafeSet(int index, T value)
        {
            while (items.Count <= index)
            {
                items.Add(null);
            }

            if (items[index] == null)
                ++Count;
            if (value == null)
                --Count;

            items[index] = value;
            keys = null;
            values = null;
        }

        private class Enumerator : IEnumerator<KeyValuePair<int, T>>
        {
            private int index = -1;
            private readonly IEnumerator<T> enumerator;

            public Enumerator(IEnumerator<T> enumerator)
            {
                this.enumerator = enumerator;
            }

            public void Dispose()
            {
                index = -1;
                enumerator.Dispose();
            }

            public bool MoveNext()
            {
                var result = false;
                while (!result)
                {
                    ++index;
                    if (!enumerator.MoveNext())
                        return false;

                    if (enumerator.Current != null)
                        result = true;
                }
                return true;
            }

            public void Reset()
            {
                index = -1;
                enumerator.Reset();
            }

            public KeyValuePair<int, T> Current => new KeyValuePair<int, T>(index, enumerator.Current);

            object IEnumerator.Current => Current;
        }
    }
}
