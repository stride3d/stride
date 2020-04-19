// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using Stride.Core.Annotations;
using Stride.Core.Serialization;

namespace Stride.Core.Storage
{
    /// <summary>
    /// A hash to uniquely identify data.
    /// </summary>
    public partial struct ObjectId
    {
        /// <summary>
        /// Computes a hash from an object using <see cref="BinarySerializationWriter"/>.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The hash of the object.</returns>
        public static ObjectId FromObject<T>(T obj)
        {
            byte[] buffer;
            return FromObject(obj, out buffer);
        }

        /// <summary>
        /// Computes a hash from an object using <see cref="BinarySerializationWriter" />.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="buffer">The buffer containing the serialized object.</param>
        /// <returns>The hash of the object.</returns>
        public static ObjectId FromObject<T>(T obj, [NotNull] out byte[] buffer)
        {
            var stream = new MemoryStream();
            var writer = new BinarySerializationWriter(stream);
            writer.Context.SerializerSelector = SerializerSelector.Asset;
            writer.Serialize(ref obj, ArchiveMode.Serialize);
            stream.Position = 0;
            buffer = stream.ToArray();
            return FromBytes(buffer);
        }
    }
}
