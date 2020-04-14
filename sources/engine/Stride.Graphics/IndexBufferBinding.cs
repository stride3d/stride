// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Graphics
{
    [DataSerializer(typeof(IndexBufferBinding.Serializer))]
    public class IndexBufferBinding
    {
        public IndexBufferBinding(Buffer indexBuffer, bool is32Bit, int count, int indexOffset = 0)
        {
            if (indexBuffer == null) throw new ArgumentNullException("indexBuffer");
            Buffer = indexBuffer;
            Is32Bit = is32Bit;
            Offset = indexOffset;
            Count = count;
        }

        public Buffer Buffer { get; private set; }
        public bool Is32Bit { get; private set; }
        public int Offset { get; private set; }

        public int Count { get; private set; }

        internal class Serializer : DataSerializer<IndexBufferBinding>
        {
            public override void Serialize(ref IndexBufferBinding indexBufferBinding, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Deserialize)
                {
                    var buffer = stream.Read<Buffer>();
                    var is32Bit = stream.ReadBoolean();
                    var count = stream.ReadInt32();
                    var offset = stream.ReadInt32();

                    indexBufferBinding = new IndexBufferBinding(buffer, is32Bit, count, offset);
                }
                else
                {
                    stream.Write(indexBufferBinding.Buffer);
                    stream.Write(indexBufferBinding.Is32Bit);
                    stream.Write(indexBufferBinding.Count);
                    stream.Write(indexBufferBinding.Offset);
                }
            }
        }
    }
}
