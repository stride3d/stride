// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core.Serialization;

namespace Stride.Graphics;

[DataSerializer(typeof(VertexBufferBinding.Serializer))]
public readonly struct VertexBufferBinding : IEquatable<VertexBufferBinding>
{
    private readonly int hashCode;
    /// <summary>
    /// Binding structure that specifies a vertex buffer and other per-vertex parameters (such as offset and instancing) for a graphics device.
    /// </summary>
    public VertexBufferBinding(Buffer vertexBuffer, VertexDeclaration vertexDeclaration, int vertexCount, int vertexStride = -1, int vertexOffset = 0) : this()
    {
        ArgumentNullException.ThrowIfNull(vertexBuffer);
        ArgumentNullException.ThrowIfNull(vertexDeclaration);

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexBufferBinding"/> struct.
        /// </summary>
        /// <param name="vertexStride">Jump size to the next element. if -1, it gets auto-discovered from the vertexDeclaration</param>
        /// <param name="vertexOffset">Offset (in Vertex ElementCount) from the beginning of the buffer to the first vertex to use.</param>
        Buffer = vertexBuffer;
        Stride = vertexStride != -1 ? vertexStride : vertexDeclaration.VertexStride;
        Offset = vertexOffset;
        Count = vertexCount;
        Declaration = vertexDeclaration;

        hashCode = HashCode.Combine(vertexBuffer, vertexOffset, vertexStride, vertexCount, vertexDeclaration);
    }


        /// <summary>
        /// Gets a vertex buffer.
        /// </summary>
    public Buffer Buffer { get; }

        /// <summary>
        /// Gets the offset in bytes between the beginning of the buffer and the vertex data to use.
        /// </summary>
    public int Offset { get; }

        /// <summary>
        /// Gets the vertex stride.
        /// </summary>
    public int Stride { get; }

        /// <summary>
        /// Gets the number of vertex.
        /// </summary>
        /// <value>The count.</value>
    public int Count { get; }

        /// <summary>
        /// Gets the layout of the vertex buffer.
        /// </summary>
        /// <value>The declaration.</value>
    public VertexDeclaration Declaration { get; }


    public readonly bool Equals(VertexBufferBinding other)
    {
        return Buffer.Equals(other.Buffer)
            && Offset == other.Offset
            && Stride == other.Stride
            && Count == other.Count
            && Declaration.Equals(other.Declaration);
    }

    public override readonly bool Equals(object obj)
    {
        return obj is VertexBufferBinding vbb && Equals(vbb);
    }

    public static bool operator ==(VertexBufferBinding left, VertexBufferBinding right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VertexBufferBinding left, VertexBufferBinding right)
    {
        return !(left == right);
    }

    public override readonly int GetHashCode() => hashCode;

    #region Serializer

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

        #endregion
    }
}
