// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Serializers;

namespace Xenko.Graphics
{    
    /// <summary>
    /// Binding structure that specifies a vertex buffer and other per-vertex parameters (such as offset and instancing) for a graphics device.
    /// </summary>
    [DataSerializer(typeof(VertexBufferBinding.Serializer))]
    public struct VertexBufferBinding : IEquatable<VertexBufferBinding>
    {
        private readonly int hashCode;

        /// <param name="vertexStride">Jump size to the next element. if -1, it gets auto-discovered from the vertexDeclaration</param>
        /// <param name="vertexOffset">Offset (in Vertex ElementCount) from the beginning of the buffer to the first vertex to use.</param>
        public VertexBufferBinding(Buffer vertexBuffer, VertexDeclaration vertexDeclaration, int vertexCount, int vertexStride = -1, int vertexOffset = 0) : this()
        {
            if (vertexBuffer == null) throw new ArgumentNullException("vertexBuffer");
            if (vertexDeclaration == null) throw new ArgumentNullException("vertexDeclaration");

            Buffer = vertexBuffer;
            Stride = vertexStride != -1 ? vertexStride : vertexDeclaration.VertexStride;
            Offset = vertexOffset;
            Count = vertexCount;
            Declaration = vertexDeclaration;

            unchecked
            {
                hashCode = Buffer.GetHashCode();
                hashCode = (hashCode*397) ^ Offset;
                hashCode = (hashCode*397) ^ Stride;
                hashCode = (hashCode*397) ^ Count;
                hashCode = (hashCode*397) ^ Declaration.GetHashCode();
            }
        }

        /// <summary>
        /// Gets a vertex buffer.
        /// </summary>
        public Buffer Buffer { get; private set; }

        /// <summary>
        /// Gets the offset (vertex index) between the beginning of the buffer and the vertex data to use.
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// Gets the vertex stride.
        /// </summary>
        public int Stride { get; private set; }

        /// <summary>
        /// Gets the number of vertex.
        /// </summary>
        /// <value>The count.</value>
        public int Count { get; private set; }

        /// <summary>
        /// Gets the layout of the vertex buffer.
        /// </summary>
        /// <value>The declaration.</value>
        public VertexDeclaration Declaration { get; private set; }

        public bool Equals(VertexBufferBinding other)
        {
            return Buffer.Equals(other.Buffer) && Offset == other.Offset && Stride == other.Stride && Count == other.Count && Declaration.Equals(other.Declaration);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexBufferBinding && Equals((VertexBufferBinding)obj);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        internal class Serializer : DataSerializer<VertexBufferBinding>
        {
            public override void Serialize(ref VertexBufferBinding vertexBufferBinding, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Deserialize)
                {
                    var buffer = stream.Read<Buffer>();
                    var declaration = stream.Read<VertexDeclaration>();
                    var count = stream.ReadInt32();
                    var stride = stream.ReadInt32();
                    var offset = stream.ReadInt32();

                    vertexBufferBinding = new VertexBufferBinding(buffer, declaration, count, stride, offset);
                }
                else
                {
                    stream.Write(vertexBufferBinding.Buffer);
                    stream.Write(vertexBufferBinding.Declaration);
                    stream.Write(vertexBufferBinding.Count);
                    stream.Write(vertexBufferBinding.Stride);
                    stream.Write(vertexBufferBinding.Offset);
                }
            }
        }
    }

    public static class VertexBufferBindingExtensions
    {
        public static InputElementDescription[] CreateInputElements(this VertexDeclaration vertexDeclaration)
        {
            var inputElements = new InputElementDescription[vertexDeclaration.VertexElements.Length];
            var inputElementIndex = 0;
            foreach (var element in vertexDeclaration.EnumerateWithOffsets())
            {
                inputElements[inputElementIndex++] = new InputElementDescription
                {
                    SemanticName = element.VertexElement.SemanticName,
                    SemanticIndex = element.VertexElement.SemanticIndex,
                    Format = element.VertexElement.Format,
                    InputSlot = 0,
                    AlignedByteOffset = element.Offset,
                };
            }

            return inputElements;
        }

        public static InputElementDescription[] CreateInputElements(this VertexBufferBinding[] vertexBuffers)
        {
            var inputElementCount = 0;
            foreach (var vertexBuffer in vertexBuffers)
            {
                inputElementCount += vertexBuffer.Declaration.VertexElements.Length;
            }

            var inputElements = new InputElementDescription[inputElementCount];
            var inputElementIndex = 0;
            for (int inputSlot = 0; inputSlot < vertexBuffers.Length; inputSlot++)
            {
                var vertexBuffer = vertexBuffers[inputSlot];
                foreach (var element in vertexBuffer.Declaration.EnumerateWithOffsets())
                {
                    inputElements[inputElementIndex++] = new InputElementDescription
                    {
                        SemanticName = element.VertexElement.SemanticName,
                        SemanticIndex = element.VertexElement.SemanticIndex,
                        Format = element.VertexElement.Format,
                        InputSlot = inputSlot,
                        AlignedByteOffset = element.Offset,
                    };
                }
            }

            return inputElements;
        }
    }
}
