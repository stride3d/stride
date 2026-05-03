// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics
{
    /// <summary>
    ///   Describes a GPU <see cref="Buffer"/>.
    /// </summary>
    public struct BufferDescription : IEquatable<BufferDescription>
    {
        /// <summary>
        ///   Initializes a new instance of <see cref="BufferDescription"/> struct.
        /// </summary>
        /// <param name="sizeInBytes">Size of the Buffer in bytes.</param>
        /// <param name="bufferFlags">Buffer flags describing the type of Buffer.</param>
        /// <param name="usage">The usage for the Buffer, which determines who can read/write data.</param>
        /// <param name="structureByteStride">
        ///   <para>
        ///     If the Buffer is a <strong>Structured Buffer</strong> or a <strong>Typed Buffer</strong>, this parameter indicates
        ///     the <strong>stride</strong> of each element of the Buffer (the structure). The stride is not only the size of the
        ///     structure, but also any padding in between two consecutive elements.
        ///   </para>
        ///   <para>For any other kind of Buffer, this parameter can be 0.</para>
        /// </param>
        public BufferDescription(int sizeInBytes, BufferFlags bufferFlags, GraphicsResourceUsage usage, int structureByteStride = 0)
        {
            SizeInBytes = sizeInBytes;
            BufferFlags = bufferFlags;
            Usage = usage;
            StructureByteStride = structureByteStride;
        }


        /// <summary>
        ///   Size of the <see cref="Buffer"/> in bytes.
        /// </summary>
        public int SizeInBytes;

        /// <summary>
        ///   Flags describing the type of <see cref="Buffer"/>.
        /// </summary>
        public BufferFlags BufferFlags;

        /// <summary>
        ///   Usage for the <see cref="Buffer"/>, which determines who can read / write data.
        /// </summary>
        public GraphicsResourceUsage Usage;

        /// <summary>
        ///   The size in bytes of the structure (each element in the <see cref="Buffer"/>) when it represents a
        ///   <strong>Structured Buffer</strong> or a <strong>Typed Buffer</strong>.
        /// </summary>
        public int StructureByteStride;


        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(SizeInBytes, BufferFlags, Usage, StructureByteStride);
        }

        public static bool operator ==(BufferDescription left, BufferDescription right) => left.Equals(right);

        public static bool operator !=(BufferDescription left, BufferDescription right) => !left.Equals(right);
    }
}
