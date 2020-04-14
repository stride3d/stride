// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Graphics
{
    /// <summary>
    /// Describes a buffer.
    /// </summary>
    public struct BufferDescription : IEquatable<BufferDescription>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BufferDescription"/> struct.
        /// </summary>
        /// <param name="sizeInBytes">Size of the buffer in bytes.</param>
        /// <param name="bufferFlags">Buffer flags describing the type of buffer.</param>
        /// <param name="usage">Usage of this buffer.</param>
        /// <param name="structureByteStride">The size of the structure (in bytes) when it represents a structured/typed buffer. Default = 0.</param>
        public BufferDescription(int sizeInBytes, BufferFlags bufferFlags, GraphicsResourceUsage usage, int structureByteStride = 0)
        {
            SizeInBytes = sizeInBytes;
            BufferFlags = bufferFlags;
            Usage = usage;
            StructureByteStride = structureByteStride;
        }

        /// <summary>
        /// Size of the buffer in bytes.
        /// </summary>
        public int SizeInBytes;

        /// <summary>
        /// Buffer flags describing the type of buffer.
        /// </summary>
        public BufferFlags BufferFlags;

        /// <summary>
        /// Usage of this buffer.
        /// </summary>
        public GraphicsResourceUsage Usage;

        /// <summary>
        /// The size of the structure (in bytes) when it represents a structured/typed buffer.
        /// </summary>
        public int StructureByteStride;

        public bool Equals(BufferDescription other)
        {
            return SizeInBytes == other.SizeInBytes && BufferFlags == other.BufferFlags && Usage == other.Usage && StructureByteStride == other.StructureByteStride;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is BufferDescription && Equals((BufferDescription)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = SizeInBytes;
                hashCode = (hashCode * 397) ^ (int)BufferFlags;
                hashCode = (hashCode * 397) ^ (int)Usage;
                hashCode = (hashCode * 397) ^ StructureByteStride;
                return hashCode;
            }
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(BufferDescription left, BufferDescription right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(BufferDescription left, BufferDescription right)
        {
            return !left.Equals(right);
        }
    }
}
