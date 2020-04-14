// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
using System;

namespace Xenko.Core.Serialization.Contents
{
    /// <summary>
    /// ContentSerializer that simply defers serialization to low level serialization.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    public class DataContentSerializer<T> : ContentSerializerBase<T>
    {
        private readonly DataContentSerializerHelper<T> dataSerializerHelper = new DataContentSerializerHelper<T>();

        public override void Serialize(ContentSerializerContext context, SerializationStream stream, T obj)
        {
            dataSerializerHelper.Serialize(context, stream, obj);
        }
    }

    public class DataContentSerializerHelper<T>
    {
        private DataSerializer<T> dataSerializer;

        public void Serialize(ContentSerializerContext context, SerializationStream stream, T obj)
        {
            // Get serializer
            // Note: Currently registered serializer is the content reference one
            // However, we would like to serialize the actual type here
            if (dataSerializer == null)
            {
                var dataSerializerType = DataSerializerFactory.GetSerializer("Default", typeof(T)).SerializerType;
                if (dataSerializerType == null)
                    throw new InvalidOperationException($"Could not find a serializer for type {typeof(T)}");
                dataSerializer = (DataSerializer<T>)Activator.CreateInstance(dataSerializerType);
                dataSerializer.Initialize(stream.Context.SerializerSelector);
            }

            // Serialize object
            stream.SerializeExtended(ref obj, context.Mode, dataSerializer);
        }
    }
}
