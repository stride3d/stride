// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core.Serialization;

namespace Stride.Graphics
{
    /// <summary>
    ///   Binding structure that specifies an Index Buffer for a Graphics Device.
    /// </summary>
    [DataSerializer(typeof(IndexBufferBinding.Serializer))]
    public class IndexBufferBinding : IEquatable<IndexBufferBinding>
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="IndexBufferBinding"/> class.
        /// </summary>
        /// <param name="indexBuffer">The Index Buffer to bind.</param>
        /// <param name="is32Bit">
        ///   A value indicating if the indices are 16-bit (<see langword="false"/>), or 32-bit (<see langword="true"/>).
        /// </param>
        /// <param name="indexCount">The number of indices in the Buffer to use.</param>
        /// <param name="vertexOffset">
        ///   The offset (in number of indices) from the beginning of the Buffer to the first index to use.
        ///   Default is <c>0</c>, meaning the first index in the Buffer will be used.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="indexBuffer"/> is <see langword="null"/>.
        /// </exception>
        public IndexBufferBinding(Buffer indexBuffer, bool is32Bit, int indexCount, int indexOffset = 0)
        {
            ArgumentNullException.ThrowIfNull(indexBuffer);

            Buffer = indexBuffer;
            Is32Bit = is32Bit;     // TODO: Should we use the enum IndexElementSize instead?
            Offset = indexOffset;
            Count = indexCount;
        }


        /// <summary>
        ///   Gets the Index Buffer to bind.
        /// </summary>
        public Buffer Buffer { get; private set; }

        /// <summary>
        ///   Gets a value indicating if the Buffer contains 32-bit indices.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Buffer contains 32-bit indices;
        ///   <see langword="false"/> if the Buffer contains 16-bit indices.
        /// </value>
        public bool Is32Bit { get; private set; }

        /// <summary>
        ///   Gets the offset (in number of indices) from the beginning of the <see cref="Buffer"/> to the first index to use.
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        ///   Gets the number of indices in the Buffer to use.
        /// </summary>
        public int Count { get; private set; }


        /// <inheritdoc/>
        public bool Equals(IndexBufferBinding other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Buffer.Equals(other.Buffer)
                && Offset == other.Offset
                && Count == other.Count
                && Is32Bit == other.Is32Bit;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is IndexBufferBinding ibb && Equals(ibb);
        }

        public static bool operator ==(IndexBufferBinding left, IndexBufferBinding right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IndexBufferBinding left, IndexBufferBinding right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Buffer, Is32Bit, Count, Offset);
        }

        #region Serializer

        /// <summary>
        ///   Provides functionality to serialize and deserialize <see cref="IndexBufferBinding"/> objects.
        /// </summary>
        internal class Serializer : DataSerializer<IndexBufferBinding>
        {
            /// <summary>
            ///   Serializes or deserializes a <see cref="IndexBufferBinding"/> object.
            /// </summary>
            /// <param name="indexBufferBinding">The object to serialize or deserialize.</param>
            /// <inheritdoc/>
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

        #endregion
    }
}
