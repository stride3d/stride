// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using Stride.Core.Annotations;
using Stride.Core.IO;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Implements <see cref="SerializationStream"/> as a binary writer.
    /// </summary>
    public class BinarySerializationWriter : SerializationStream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinarySerializationWriter"/> class.
        /// </summary>
        /// <param name="outputStream">The output stream.</param>
        public BinarySerializationWriter([NotNull] Stream outputStream)
        {
            Writer = new BinaryWriter(outputStream);
            NativeStream = outputStream.ToNativeStream();
        }

        private BinaryWriter Writer { get; }

        /// <inheritdoc />
        public override void Serialize(ref bool value)
        {
            NativeStream.WriteByte(value ? (byte)1 : (byte)0);
        }

        /// <inheritdoc />
        public override unsafe void Serialize(ref float value)
        {
            fixed (float* valuePtr = &value)
                NativeStream.Write(*(uint*)valuePtr);
        }

        /// <inheritdoc />
        public override unsafe void Serialize(ref double value)
        {
            fixed (double* valuePtr = &value)
                NativeStream.Write(*(ulong*)valuePtr);
        }

        /// <inheritdoc />
        public override void Serialize(ref short value)
        {
            NativeStream.Write((ushort)value);
        }

        /// <inheritdoc />
        public override void Serialize(ref int value)
        {
            NativeStream.Write((uint)value);
        }

        /// <inheritdoc />
        public override void Serialize(ref long value)
        {
            NativeStream.Write((ulong)value);
        }

        /// <inheritdoc />
        public override void Serialize(ref ushort value)
        {
            NativeStream.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref uint value)
        {
            NativeStream.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref ulong value)
        {
            NativeStream.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref string value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref char value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref byte value)
        {
            NativeStream.WriteByte(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref sbyte value)
        {
            NativeStream.WriteByte((byte)value);
        }

        /// <inheritdoc />
        public override void Serialize([NotNull] byte[] values, int offset, int count)
        {
            NativeStream.Write(values, offset, count);
        }

        /// <inheritdoc/>
        public override void Serialize(IntPtr memory, int count)
        {
            NativeStream.Write(memory, count);
        }

        /// <inheritdoc />
        public override void Flush()
        {
            NativeStream.Flush();
        }
    }
}
