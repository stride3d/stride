// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Xenko.Core;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Serializers;

namespace Xenko.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// A custom dictionary to keep track of the order the elements were inserted.
    /// </summary>
    [DataSerializer(typeof(ComputeColorParameters.Serializer))]
    [DataContract("ComputeColorParameters")]
    public class ComputeColorParameters : IDictionary<string, IComputeColorParameter>
    {
        private readonly List<KeyValuePair<string, IComputeColorParameter>> internalDictionary;

        public ComputeColorParameters()
        {
            internalDictionary = new List<KeyValuePair<string, IComputeColorParameter>>();
        }

        //TODO: custom enumerator?
        public IEnumerator<KeyValuePair<string, IComputeColorParameter>> GetEnumerator()
        {
            return internalDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, IComputeColorParameter> item)
        {
            internalDictionary.Add(item);
        }

        public void Clear()
        {
            internalDictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, IComputeColorParameter> item)
        {
            return internalDictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, IComputeColorParameter>[] array, int arrayIndex)
        {
            var copyCount = Math.Min(array.Length - arrayIndex, internalDictionary.Count);
            for (var i = 0; i < copyCount; ++i)
            {
                array[arrayIndex + i] = internalDictionary[i];
            }
        }

        public bool Remove(KeyValuePair<string, IComputeColorParameter> item)
        {
            return internalDictionary.Remove(item);
        }

        public int Count
        {
            get
            {
                return internalDictionary.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool ContainsKey(string key)
        {
            return internalDictionary.Any(x => x.Key == key);
        }

        public void Add(string key, IComputeColorParameter value)
        {
            internalDictionary.Add(new KeyValuePair<string, IComputeColorParameter>(key, value));
        }

        public bool Remove(string key)
        {
            if (ContainsKey(key))
            {
                internalDictionary.RemoveAll(x => x.Key == key);
                return true;
            }
            return false;
        }

        public bool TryGetValue(string key, out IComputeColorParameter value)
        {
            if (ContainsKey(key))
            {
                value = this[key];
                return true;
            }
            value = null;
            return false;
        }

        public IComputeColorParameter this[string key]
        {
            get
            {
                var found = internalDictionary.FirstOrDefault(x => x.Key == key).Value;
                if (found != null)
                    return found;
                throw new KeyNotFoundException();
            }
            set
            {
                var newValue = new KeyValuePair<string, IComputeColorParameter>(key, value);
                var foundIndex = internalDictionary.FindIndex(x => x.Key == key);
                if (foundIndex >= 0)
                    internalDictionary[foundIndex] = newValue;
                else
                    internalDictionary.Add(newValue);
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                return internalDictionary.Select(x => x.Key).ToList();
            }
        }

        public ICollection<IComputeColorParameter> Values
        {
            get
            {
                return internalDictionary.Select(x => x.Value).ToList();
            }
        }

        internal class Serializer : DataSerializer<ComputeColorParameters>, IDataSerializerGenericInstantiation
        {
            private DataSerializer<KeyValuePair<string, IComputeColorParameter>> itemDataSerializer;

            /// <inheritdoc/>
            public override void Initialize(SerializerSelector serializerSelector)
            {
                itemDataSerializer = serializerSelector.GetSerializer<KeyValuePair<string, IComputeColorParameter>>();
            }

            public override void PreSerialize(ref ComputeColorParameters obj, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Deserialize)
                {
                    // TODO: Peek the dictionary size
                    if (obj == null)
                        obj = new ComputeColorParameters();
                    else
                        obj.Clear();
                }
            }

            /// <inheritdoc/>
            public override void Serialize(ref ComputeColorParameters obj, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Deserialize)
                {
                    // Should be null if it was
                    int count = stream.ReadInt32();
                    for (int i = 0; i < count; ++i)
                    {
                        var value = new KeyValuePair<string, IComputeColorParameter>();
                        itemDataSerializer.Serialize(ref value, mode, stream);
                        obj.Add(value.Key, value.Value);
                    }
                }
                else if (mode == ArchiveMode.Serialize)
                {
                    stream.Write(obj.Count);
                    foreach (var item in obj.internalDictionary)
                    {
                        itemDataSerializer.Serialize(item, stream);
                    }
                }
            }

            public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
            {
                genericInstantiations.Add(typeof(KeyValuePair<string, IComputeColorParameter>));
            }
        }
    }
}
