// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using ServiceWire;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Core.BuildEngine
{
    public class XenkoServiceWireSerializer : ISerializer
    {
        public T Deserialize<T>(byte[] bytes)
        {
            var reader = new BinarySerializationReader(new MemoryStream(bytes));
            reader.Context.SerializerSelector = SerializerSelector.AssetWithReuse;
            reader.Context.Set(ContentSerializerContext.SerializeAttachedReferenceProperty, ContentSerializerContext.AttachedReferenceSerialization.AsSerializableVersion);
            T command = default(T);
            reader.SerializeExtended(ref command, ArchiveMode.Deserialize, null);
            return command;
        }

        public object Deserialize(byte[] bytes, string typeConfigName)
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize<T>(T obj)
        {
            var memoryStream = new MemoryStream();
            var writer = new BinarySerializationWriter(memoryStream);
            writer.Context.SerializerSelector = SerializerSelector.AssetWithReuse;
            writer.Context.Set(ContentSerializerContext.SerializeAttachedReferenceProperty, ContentSerializerContext.AttachedReferenceSerialization.AsSerializableVersion);
            writer.SerializeExtended(ref obj, ArchiveMode.Serialize);

            return memoryStream.ToArray();
        }

        public byte[] Serialize(object obj, string typeConfigName)
        {
            throw new NotImplementedException();
        }
    }
}
