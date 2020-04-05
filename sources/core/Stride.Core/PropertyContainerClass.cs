// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Core
{
    /// <summary>
    /// Class wrapper around <see cref="PropertyContainer"/>.
    /// </summary>
    [DataContract]
    [DataSerializer(typeof(DictionaryAllSerializer<PropertyContainerClass, PropertyKey, object>))]
    public class PropertyContainerClass : IDictionary<PropertyKey, object>, IReadOnlyDictionary<PropertyKey, object>
    {
        private PropertyContainer inner;

        public PropertyContainerClass()
        {
            inner = new PropertyContainer();
        }

        public PropertyContainerClass(object owner)
        {
            inner = new PropertyContainer(owner);
        }

        /// <inheritdoc />
        public int Count => inner.Count;

        /// <inheritdoc />
        public bool IsReadOnly => inner.IsReadOnly;

        /// <inheritdoc />
        public ICollection<PropertyKey> Keys => inner.Keys;

        /// <inheritdoc />
        [DataMemberIgnore]
        public object Owner => inner.Owner;

        /// <inheritdoc />
        public ICollection<object> Values => inner.Values;

        /// <inheritdoc />
        public object this[[NotNull] PropertyKey key] { get { return inner[key]; } set { inner[key] = value; } }

        /// <inheritdoc />
        public event PropertyContainer.PropertyUpdatedDelegate PropertyUpdated
        {
            add { inner.PropertyUpdated += value; }
            remove { inner.PropertyUpdated -= value; }
        }

        /// <inheritdoc />
        public void Clear()
        {
            inner.Clear();
        }

        /// <inheritdoc />
        public void Add<T>([NotNull] PropertyKey<T> key, T value)
        {
            inner.Add(key, value);
        }

        /// <inheritdoc />
        public bool ContainsKey([NotNull] PropertyKey key)
        {
            return inner.ContainsKey(key);
        }

        /// <inheritdoc />
        public bool Remove([NotNull] PropertyKey propertyKey)
        {
            return inner.Remove(propertyKey);
        }

        /// <inheritdoc />
        public void CopyTo(ref PropertyContainer destination)
        {
            inner.CopyTo(ref destination);
        }

        public void CopyTo(PropertyContainerClass destination)
        {
            inner.CopyTo(ref destination.inner);
        }

        /// <inheritdoc />
        public object Get([NotNull] PropertyKey propertyKey)
        {
            return inner.Get(propertyKey);
        }

        /// <inheritdoc />
        public T GetSafe<T>([NotNull] PropertyKey<T> propertyKey)
        {
            return inner.GetSafe(propertyKey);
        }

        /// <inheritdoc />
        public T Get<T>([NotNull] PropertyKey<T> propertyKey)
        {
            return inner.Get(propertyKey);
        }

        /// <inheritdoc />
        public bool TryGetValue(PropertyKey propertyKey, out object value)
        {
            return inner.TryGetValue(propertyKey, out value);
        }

        /// <inheritdoc />
        public bool TryGetValue<T>([NotNull] PropertyKey<T> propertyKey, out T value)
        {
            return inner.TryGetValue(propertyKey, out value);
        }

        /// <inheritdoc />
        public void Set<T>([NotNull] PropertyKey<T> propertyKey, T tagValue)
        {
            inner.Set(propertyKey, tagValue);
        }

        /// <inheritdoc />
        public void SetObject([NotNull] PropertyKey propertyKey, object tagValue)
        {
            inner.SetObject(propertyKey, tagValue);
        }

        /// <inheritdoc />
        internal void RaisePropertyContainerUpdated(PropertyKey propertyKey, object newValue, object oldValue)
        {
            inner.RaisePropertyContainerUpdated(propertyKey, newValue, oldValue);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator<KeyValuePair<PropertyKey, object>> IEnumerable<KeyValuePair<PropertyKey, object>>.GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<PropertyKey, object>>.Add(KeyValuePair<PropertyKey, object> item)
        {
            inner.SetObject(item.Key, item.Value);
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<PropertyKey, object>>.Contains(KeyValuePair<PropertyKey, object> item)
        {
            object temp;
            return inner.TryGetValue(item.Key, out temp) && Equals(temp, item.Value);
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<PropertyKey, object>>.CopyTo(KeyValuePair<PropertyKey, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<PropertyKey, object>>)inner).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<PropertyKey, object>>.Remove(KeyValuePair<PropertyKey, object> item)
        {
            return Remove(item.Key);
        }

        /// <inheritdoc />
        void IDictionary<PropertyKey, object>.Add([NotNull] PropertyKey key, object value)
        {
            inner.SetObject(key, value);
        }

        /// <inheritdoc />
        IEnumerable<PropertyKey> IReadOnlyDictionary<PropertyKey, object>.Keys => Keys;

        /// <inheritdoc />
        IEnumerable<object> IReadOnlyDictionary<PropertyKey, object>.Values => Values;
    }
}
