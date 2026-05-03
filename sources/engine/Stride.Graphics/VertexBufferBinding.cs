// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core.Serialization;

namespace Stride.Graphics;

/// <summary>
///   Binding structure that specifies a Vertex Buffer and other per-vertex parameters (such as offset and instancing) for a Graphics Device.
/// </summary>
[DataSerializer(typeof(VertexBufferBinding.Serializer))]
public readonly struct VertexBufferBinding : IEquatable<VertexBufferBinding>
{
    private readonly int hashCode;

    /// <summary>
    ///   Initializes a new instance of the <see cref="VertexBufferBinding"/> structure.
    /// </summary>
    /// <param name="vertexBuffer">The Vertex Buffer to bind.</param>
    /// <param name="vertexDeclaration">
    ///   A description of the layout of the vertices in the <paramref name="vertexBuffer"/>, defining how the data is structured.
    /// </param>
    /// <param name="vertexCount">The number of vertices in the Buffer to use.</param>
    /// <param name="vertexStride">
    ///   The size of a single vertex in bytes. This is the distance between two consecutive vertices in the buffer.
    ///   Specify <c>-1</c> to auto-discover the stride from the <paramref name="vertexDeclaration"/>.
    /// </param>
    /// <param name="vertexOffset">
    ///   The offset in bytes from the beginning of the Buffer to the first vertex to use.
    ///   Default is <c>0</c>, meaning the first vertex in the Buffer will be used.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="vertexBuffer"/> or <paramref name="vertexDeclaration"/> is <see langword="null"/>.
    /// </exception>
    public VertexBufferBinding(Buffer vertexBuffer, VertexDeclaration vertexDeclaration, int vertexCount, int vertexStride = -1, int vertexOffset = 0) : this()
    {
        ArgumentNullException.ThrowIfNull(vertexBuffer);
        ArgumentNullException.ThrowIfNull(vertexDeclaration);

        Buffer = vertexBuffer;
        Stride = vertexStride != -1 ? vertexStride : vertexDeclaration.VertexStride;
        Offset = vertexOffset;
        Count = vertexCount;
        Declaration = vertexDeclaration;

        hashCode = HashCode.Combine(vertexBuffer, vertexOffset, vertexStride, vertexCount, vertexDeclaration);
    }


    /// <summary>
    ///   Gets the Vertex Buffer to bind.
    /// </summary>
    public Buffer Buffer { get; }

    /// <summary>
    ///   Gets the offset in bytes from the beginning of the <see cref="Buffer"/> to the first vertex to use.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    ///   Gets the size of a single vertex in bytes. This is the distance between two consecutive vertices in the <see cref="Buffer"/>.
    /// </summary>
    public int Stride { get; }

    /// <summary>
    ///   Gets the number of vertices in the <see cref="Buffer"/> to use.
    /// </summary>
    public int Count { get; }

    /// <summary>
    ///   Gets a description of the layout of the vertices in the <see cref="Buffer"/>, defining how the data is structured.
    /// </summary>
    public VertexDeclaration Declaration { get; }


    /// <inheritdoc/>
    public readonly bool Equals(VertexBufferBinding other)
    {
        return Buffer.Equals(other.Buffer)
            && Offset == other.Offset
            && Stride == other.Stride
            && Count == other.Count
            && Declaration.Equals(other.Declaration);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override readonly int GetHashCode() => hashCode;

    #region Serializer

    /// <summary>
    ///   Provides functionality to serialize and deserialize <see cref="VertexBufferBinding"/> objects.
    /// </summary>
    internal class Serializer : DataSerializer<VertexBufferBinding>
    {
        /// <summary>
        ///   Serializes or deserializes a <see cref="VertexBufferBinding"/> object.
        /// </summary>
        /// <param name="vertexBufferBinding">The object to serialize or deserialize.</param>
        /// <inheritdoc/>
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
