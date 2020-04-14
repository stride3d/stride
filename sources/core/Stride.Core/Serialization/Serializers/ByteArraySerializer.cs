// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Serialization.Serializers
{
    /// <summary>
    /// Implements <see cref="DataSerializer{T}"/> for a byte array.
    /// </summary>
    [DataSerializerGlobal(typeof(ByteArraySerializer))]
    public class ByteArraySerializer : DataSerializer<byte[]>
    {
        /// <inheritdoc/>
        public override void PreSerialize(ref byte[] obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Length);
            }
            else if (mode == ArchiveMode.Deserialize)
            {
                var length = stream.ReadInt32();
                obj = new byte[length];
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref byte[] obj, ArchiveMode mode, SerializationStream stream)
        {
            stream.Serialize(obj, 0, obj.Length);
        }
    }
}
