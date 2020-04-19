// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Reflection;
using Stride.Core.Annotations;
using Stride.Core.Storage;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Simple serializer that will matches specific type using base type and create a data serializer with matched type.
    /// </summary>
    public class GenericSerializerFactory : SerializerFactory
    {
        private readonly Type baseType;
        private readonly Type serializerGenericType;
        private readonly ConcurrentDictionary<Type, DataSerializer> serializersByType = new ConcurrentDictionary<Type, DataSerializer>();
        private readonly ConcurrentDictionary<ObjectId, DataSerializer> serializersByTypeId = new ConcurrentDictionary<ObjectId, DataSerializer>();

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericSerializerFactory"/> class.
        /// </summary>
        /// <param name="baseType">The type to match.</param>
        /// <param name="serializerGenericType">The generic type that will be used to instantiate serializers.</param>
        public GenericSerializerFactory(Type baseType, Type serializerGenericType)
        {
            this.baseType = baseType;
            this.serializerGenericType = serializerGenericType;
        }

        [CanBeNull]
        public override DataSerializer GetSerializer(SerializerSelector selector, ref ObjectId typeId)
        {
            DataSerializer dataSerializer;
            serializersByTypeId.TryGetValue(typeId, out dataSerializer);
            return dataSerializer;
        }

        [CanBeNull]
        public override DataSerializer GetSerializer(SerializerSelector selector, [NotNull] Type type)
        {
            DataSerializer dataSerializer;
            if (!serializersByType.TryGetValue(type, out dataSerializer))
            {
                if (baseType.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                {
                    dataSerializer = (DataSerializer)Activator.CreateInstance(serializerGenericType.MakeGenericType(type));
                    selector.EnsureInitialized(dataSerializer);
                    serializersByTypeId.TryAdd(dataSerializer.SerializationTypeId, dataSerializer);
                }

                // Add it even if null (so that failures are cached too)
                serializersByType.TryAdd(type, dataSerializer);
            }
            return dataSerializer;
        }
    }
}
