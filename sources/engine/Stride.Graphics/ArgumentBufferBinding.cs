// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Graphics
{
    [DataSerializer(typeof(ArgumentBufferBinding.Serializer))]
    public class ArgumentBufferBinding
    {
        public ArgumentBufferBinding(Buffer indexBuffer, int alignedByteOffset = 0)
        {
            if (indexBuffer == null) throw new ArgumentNullException("argmentBuffer");
            Buffer = indexBuffer;
            AlignedByteOffset = alignedByteOffset;
        }

        public Buffer Buffer { get; private set; }
        public int AlignedByteOffset { get; private set; }

        internal class Serializer : DataSerializer<ArgumentBufferBinding>
        {
            public override void Serialize(ref ArgumentBufferBinding argumentBufferBinding, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Deserialize)
                {
                    var buffer = stream.Read<Buffer>();
                    var offset = stream.ReadInt32();

                    argumentBufferBinding = new ArgumentBufferBinding(buffer, offset);
                }
                else
                {
                    stream.Write(argumentBufferBinding.Buffer);
                    stream.Write(argumentBufferBinding.AlignedByteOffset);
                }
            }
        }
    }
}
