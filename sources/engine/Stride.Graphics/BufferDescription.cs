// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics
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


        public readonly bool Equals(BufferDescription other)
        {
            return SizeInBytes == other.SizeInBytes
                && BufferFlags == other.BufferFlags
                && Usage == other.Usage
                && StructureByteStride == other.StructureByteStride;
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object obj)
        {
            if (obj is null)
                return false;

            return obj is BufferDescription description && Equals(description);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(SizeInBytes, BufferFlags, Usage, StructureByteStride);
        }

        public static bool operator ==(BufferDescription left, BufferDescription right) => left.Equals(right);

        public static bool operator !=(BufferDescription left, BufferDescription right) => !left.Equals(right);
    }
}
