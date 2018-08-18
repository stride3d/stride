// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name
using System;
using System.Collections.Generic;
using System.Reflection;
using Xenko.Core.Annotations;

namespace Xenko.Core.Serialization.Serializers
{
    /// <summary>
    /// Data serializer for List{T}.
    /// </summary>
    /// <typeparam name="T">Generics type of List{T}.</typeparam>
    [DataSerializerGlobal(typeof(ListSerializer<>), typeof(List<>), DataSerializerGenericMode.GenericArguments)]
    public class ListSerializer<T> : DataSerializer<List<T>>, IDataSerializerGenericInstantiation
    {
        private DataSerializer<T> itemDataSerializer;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            itemDataSerializer = MemberSerializer<T>.Create(serializerSelector);
        }

        /// <inheritdoc/>
        public override void PreSerialize(ref List<T> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                if (obj == null)
                    obj = new List<T>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref List<T> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                var count = stream.ReadInt32();
                obj.Capacity = count;
                for (var i = 0; i < count; ++i)
                {
                    var value = default(T);
                    itemDataSerializer.Serialize(ref value, mode, stream);
                    obj.Add(value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (var item in obj)
                {
                    itemDataSerializer.Serialize(item, stream);
                }
            }
        }

        /// <inheritdoc/>
        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, [NotNull] IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(T));
        }
    }

    /// <summary>
    /// Data serializer for IList{T}.
    /// </summary>
    /// <typeparam name="TList">Type of IList{T}.</typeparam>
    /// <typeparam name="T">Generics type of IList{T}.</typeparam>
    public class ListAllSerializer<TList, T> : DataSerializer<TList>, IDataSerializerGenericInstantiation where TList : class, IList<T>
    {
        private readonly bool isInterface = typeof(TList).GetTypeInfo().IsInterface;
        private DataSerializer<T> itemDataSerializer;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            itemDataSerializer = MemberSerializer<T>.Create(serializerSelector);
        }

        /// <inheritdoc/>
        public override void PreSerialize(ref TList obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                if (obj == null)
                    obj = isInterface ? (TList)(object)new List<T>() : Activator.CreateInstance<TList>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref TList obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                var count = stream.ReadInt32();
                for (var i = 0; i < count; ++i)
                {
                    var value = default(T);
                    itemDataSerializer.Serialize(ref value, mode, stream);
                    obj.Add(value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (var item in obj)
                {
                    itemDataSerializer.Serialize(item, stream);
                }
            }
        }

        /// <inheritdoc/>
        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, [NotNull] IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(T));
        }
    }

    /// <summary>
    /// Data serializer for SortedList{TKey, TValue}.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in SortedList{TKey, TValue}.</typeparam>
    /// <typeparam name="TValue">The type of the value in SortedList{TKey, TValue}.</typeparam>
    [DataSerializerGlobal(typeof(SortedListSerializer<,>), typeof(Xenko.Core.Collections.SortedList<,>), DataSerializerGenericMode.GenericArguments)]
    public class SortedListSerializer<TKey, TValue> : DataSerializer<Xenko.Core.Collections.SortedList<TKey, TValue>>, IDataSerializerGenericInstantiation
    {
        private DataSerializer<TKey> keySerializer;
        private DataSerializer<TValue> valueSerializer;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            // Key should never be null
            keySerializer = MemberSerializer<TKey>.Create(serializerSelector, false);
            valueSerializer = MemberSerializer<TValue>.Create(serializerSelector);
        }

        /// <inheritdoc/>
        public override void PreSerialize(ref Xenko.Core.Collections.SortedList<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // TODO: Peek the SortedList size
                if (obj == null)
                    obj = new Xenko.Core.Collections.SortedList<TKey, TValue>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref Xenko.Core.Collections.SortedList<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // Should be null if it was
                var count = stream.ReadInt32();
                for (var i = 0; i < count; ++i)
                {
                    var key = default(TKey);
                    var value = default(TValue);
                    keySerializer.Serialize(ref key, mode, stream);
                    valueSerializer.Serialize(ref value, mode, stream);
                    obj.Add(key, value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (var item in obj)
                {
                    keySerializer.Serialize(item.Key, stream);
                    valueSerializer.Serialize(item.Value, stream);
                }
            }
        }

        /// <inheritdoc/>
        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, [NotNull] IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(TKey));
            genericInstantiations.Add(typeof(TValue));
        }
    }

    /// <summary>
    /// Data serializer for IList{T}.
    /// </summary>
    /// <typeparam name="T">Generics type of IList{T}.</typeparam>
    [DataSerializerGlobal(typeof(ListInterfaceSerializer<>), typeof(IList<>), DataSerializerGenericMode.GenericArguments)]
    public class ListInterfaceSerializer<T> : DataSerializer<IList<T>>, IDataSerializerGenericInstantiation
    {
        private DataSerializer<T> itemDataSerializer;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            itemDataSerializer = MemberSerializer<T>.Create(serializerSelector);
        }

        /// <inheritdoc/>
        public override void PreSerialize(ref IList<T> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                if (obj == null)
                    obj = new List<T>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref IList<T> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                var count = stream.ReadInt32();
                for (var i = 0; i < count; ++i)
                {
                    var value = default(T);
                    itemDataSerializer.Serialize(ref value, mode, stream);
                    obj.Add(value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (var item in obj)
                {
                    itemDataSerializer.Serialize(item, stream);
                }
            }
        }

        /// <inheritdoc/>
        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, [NotNull] IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(T));

            // Force concrete type to be implemented (that's what will likely be used with this interface)
            genericInstantiations.Add(typeof(List<T>));
        }
    }

    /// <summary>
    /// Data serializer for T[].
    /// </summary>
    /// <typeparam name="T">Generics type of T[].</typeparam>
    public class ArraySerializer<T> : DataSerializer<T[]>, IDataSerializerGenericInstantiation
    {
        private DataSerializer<T> itemDataSerializer;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            itemDataSerializer = MemberSerializer<T>.Create(serializerSelector);
        }

        /// <inheritdoc/>
        public override void PreSerialize(ref T[] obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Length);
            }
            else if (mode == ArchiveMode.Deserialize)
            {
                var length = stream.ReadInt32();
                obj = new T[length];
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref T[] obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                var count = obj.Length;
                for (var i = 0; i < count; ++i)
                {
                    itemDataSerializer.Serialize(ref obj[i], mode, stream);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                var count = obj.Length;
                for (var i = 0; i < count; ++i)
                {
                    itemDataSerializer.Serialize(ref obj[i], mode, stream);
                }
            }
        }

        /// <inheritdoc/>
        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, [NotNull] IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(T));
        }
    }

    /// <summary>
    /// Data serializer for blittable T[].
    /// </summary>
    /// <typeparam name="T">Generics type of T[].</typeparam>
    public class BlittableArraySerializer<T> : ArraySerializer<T>
    {
        private int elementSize;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            elementSize = Interop.SizeOf<T>();
        }

        /// <inheritdoc/>
        public override unsafe void Serialize(ref T[] obj, ArchiveMode mode, SerializationStream stream)
        {
            var size = obj.Length * elementSize;
            var objPinned = Interop.Fixed(obj);
            if (mode == ArchiveMode.Deserialize)
            {
                stream.NativeStream.Read((IntPtr)objPinned, size);
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.NativeStream.Write((IntPtr)objPinned, size);
            }
        }
    }

    /// <summary>
    /// Data serializer for KeyValuePair{TKey, TValue}.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in KeyValuePair{TKey, TValue}.</typeparam>
    /// <typeparam name="TValue">The type of the value in KeyValuePair{TKey, TValue}.</typeparam>
    [DataSerializerGlobal(typeof(KeyValuePairSerializer<,>), typeof(KeyValuePair<,>), DataSerializerGenericMode.GenericArguments)]
    public class KeyValuePairSerializer<TKey, TValue> : DataSerializer<KeyValuePair<TKey, TValue>>, IDataSerializerGenericInstantiation
    {
        private DataSerializer<TKey> keySerializer;
        private DataSerializer<TValue> valueSerializer;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            // Key should never be null
            keySerializer = MemberSerializer<TKey>.Create(serializerSelector);
            valueSerializer = MemberSerializer<TValue>.Create(serializerSelector);
        }

        /// <inheritdoc/>
        public override void Serialize(ref KeyValuePair<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                var key = default(TKey);
                var value = default(TValue);
                keySerializer.Serialize(ref key, mode, stream);
                valueSerializer.Serialize(ref value, mode, stream);
                obj = new KeyValuePair<TKey, TValue>(key, value);
            }
            else if (mode == ArchiveMode.Serialize)
            {
                keySerializer.Serialize(obj.Key, stream);
                valueSerializer.Serialize(obj.Value, stream);
            }
        }

        /// <inheritdoc/>
        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, [NotNull] IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(TKey));
            genericInstantiations.Add(typeof(TValue));
        }
    }

    /// <summary>
    /// Data serializer for Dictionary{TKey, TValue}.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in Dictionary{TKey, TValue}.</typeparam>
    /// <typeparam name="TValue">The type of the value in Dictionary{TKey, TValue}.</typeparam>
    [DataSerializerGlobal(typeof(DictionarySerializer<,>), typeof(Dictionary<,>), DataSerializerGenericMode.GenericArguments)]
    public class DictionarySerializer<TKey, TValue> : DataSerializer<Dictionary<TKey, TValue>>, IDataSerializerGenericInstantiation
    {
        private DataSerializer<TKey> keySerializer;
        private DataSerializer<TValue> valueSerializer;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            // Key should never be null
            keySerializer = MemberSerializer<TKey>.Create(serializerSelector, false);
            valueSerializer = MemberSerializer<TValue>.Create(serializerSelector);
        }

        /// <inheritdoc/>
        public override void PreSerialize(ref Dictionary<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // TODO: Peek the dictionary size
                if (obj == null)
                    obj = new Dictionary<TKey, TValue>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref Dictionary<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // Should be null if it was
                var count = stream.ReadInt32();
                for (var i = 0; i < count; ++i)
                {
                    var key = default(TKey);
                    var value = default(TValue);
                    keySerializer.Serialize(ref key, mode, stream);
                    valueSerializer.Serialize(ref value, mode, stream);
                    obj.Add(key, value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (var item in obj)
                {
                    keySerializer.Serialize(item.Key, stream);
                    valueSerializer.Serialize(item.Value, stream);
                }
            }
        }

        /// <inheritdoc/>
        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, [NotNull] IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(TKey));
            genericInstantiations.Add(typeof(TValue));
        }
    }

    public class DictionaryAllSerializer<TDictionary, TKey, TValue> : DataSerializer<TDictionary>, IDataSerializerGenericInstantiation where TDictionary : IDictionary<TKey, TValue>
    {
        private readonly bool isInterface = typeof(TDictionary).GetTypeInfo().IsInterface;
        private DataSerializer<TKey> keySerializer;
        private DataSerializer<TValue> valueSerializer;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            // Key should never be null
            keySerializer = MemberSerializer<TKey>.Create(serializerSelector, false);
            valueSerializer = MemberSerializer<TValue>.Create(serializerSelector);
        }

        /// <inheritdoc/>
        public override void PreSerialize(ref TDictionary obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // TODO: Peek the dictionary size
                if (obj == null)
                    obj = isInterface ? (TDictionary)(object)new Dictionary<TKey, TValue>() : Activator.CreateInstance<TDictionary>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref TDictionary obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // Should be null if it was
                var count = stream.ReadInt32();
                for (var i = 0; i < count; ++i)
                {
                    var key = default(TKey);
                    var value = default(TValue);
                    keySerializer.Serialize(ref key, mode, stream);
                    valueSerializer.Serialize(ref value, mode, stream);
                    obj.Add(key, value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (var item in obj)
                {
                    keySerializer.Serialize(item.Key, stream);
                    valueSerializer.Serialize(item.Value, stream);
                }
            }
        }

        /// <inheritdoc/>
        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, [NotNull] IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(TKey));
            genericInstantiations.Add(typeof(TValue));
        }
    }

    /// <summary>
    /// Data serializer for IDictionary{TKey, TValue}.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in IDictionary{TKey, TValue}.</typeparam>
    /// <typeparam name="TValue">The type of the value in IDictionary{TKey, TValue}.</typeparam>
    [DataSerializerGlobal(typeof(DictionaryInterfaceSerializer<,>), typeof(IDictionary<,>), DataSerializerGenericMode.GenericArguments)]
    public class DictionaryInterfaceSerializer<TKey, TValue> : DataSerializer<IDictionary<TKey, TValue>>, IDataSerializerGenericInstantiation
    {
        private DataSerializer<TKey> keySerializer;
        private DataSerializer<TValue> valueSerializer;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            // Key should never be null
            keySerializer = MemberSerializer<TKey>.Create(serializerSelector, false);
            valueSerializer = MemberSerializer<TValue>.Create(serializerSelector);
        }

        /// <inheritdoc/>
        public override void PreSerialize(ref IDictionary<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // TODO: Peek the dictionary size
                if (obj == null)
                    obj = new Dictionary<TKey, TValue>();
                else
                    obj.Clear();
            }
        }
        
        /// <inheritdoc/>
        public override void Serialize(ref IDictionary<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // Should be null if it was
                var count = stream.ReadInt32();
                for (var i = 0; i < count; ++i)
                {
                    var key = default(TKey);
                    var value = default(TValue);
                    keySerializer.Serialize(ref key, mode, stream);
                    valueSerializer.Serialize(ref value, mode, stream);
                    obj.Add(key, value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (var item in obj)
                {
                    keySerializer.Serialize(item.Key, stream);
                    valueSerializer.Serialize(item.Value, stream);
                }
            }
        }

        /// <inheritdoc/>
        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, [NotNull] IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(TKey));
            genericInstantiations.Add(typeof(TValue));

            // Force concrete type to be implemented (that's what will likely be used with this interface)
            genericInstantiations.Add(typeof(Dictionary<TKey, TValue>));
        }
    }
}
