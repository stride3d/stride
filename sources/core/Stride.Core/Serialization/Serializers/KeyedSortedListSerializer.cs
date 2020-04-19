// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Collections;

namespace Stride.Core.Serialization.Serializers
{
    public class KeyedSortedListSerializer<TKeyedList, TKey, T> : DataSerializer<TKeyedList>, IDataSerializerGenericInstantiation where TKeyedList : KeyedSortedList<TKey, T>
    {
        private DataSerializer<T> itemDataSerializer;

        public override void PreSerialize(ref TKeyedList obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                if (obj == null)
                    obj = Activator.CreateInstance<TKeyedList>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            itemDataSerializer = MemberSerializer<T>.Create(serializerSelector);
        }

        /// <inheritdoc/>
        public override void Serialize(ref TKeyedList obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // TODO: We could probably avoid using TrackingKeyedList.Add, and directly fill the items list (since items are supposed to be sorted already).
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

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, [NotNull] IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(T));
        }
    }
}
