// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
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
            UnderlyingStream = outputStream;
        }

        private BinaryWriter Writer { get; }

        /// <inheritdoc />
        public override void Serialize(ref bool value)
        {
            UnderlyingStream.WriteByte(value ? (byte)1 : (byte)0);
        }

#pragma warning disable CS0618 // Type or member is obsolete
        /// <inheritdoc />
        public override unsafe void Serialize(ref float value)
        {
            UnderlyingStream.Write(Unsafe.As<float, uint>(ref value));
        }

        /// <inheritdoc />
        public override unsafe void Serialize(ref double value)
        {
            UnderlyingStream.Write(Unsafe.As<double, ulong>(ref value));
        }

        /// <inheritdoc />
        public override void Serialize(ref short value)
        {
            UnderlyingStream.Write((ushort)value);
        }

        /// <inheritdoc />
        public override void Serialize(ref int value)
        {
            UnderlyingStream.Write((uint)value);
        }

        /// <inheritdoc />
        public override void Serialize(ref long value)
        {
            UnderlyingStream.Write((ulong)value);
        }

        /// <inheritdoc />
        public override void Serialize(ref ushort value)
        {
            UnderlyingStream.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref uint value)
        {
            var buffer = new byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            UnderlyingStream.Write(buffer, 0, buffer.Length);
        }

        /// <inheritdoc />
        public override void Serialize(ref ulong value)
        {
            var buffer = new byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            UnderlyingStream.Write(buffer, 0, buffer.Length);
        }
#pragma warning restore CS0618 // Type or member is obsolete

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
            UnderlyingStream.WriteByte(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref sbyte value)
        {
            UnderlyingStream.WriteByte((byte)value);
        }

        /// <inheritdoc />
        public override void Serialize([NotNull] byte[] values, int offset, int count)
        {
            UnderlyingStream.Write(values, offset, count);
        }

        /// <inheritdoc />
        public override void Serialize(Span<byte> buffer) => UnderlyingStream.Write(buffer);

        /// <inheritdoc />
        public override void Flush()
        {
            UnderlyingStream.Flush();
        }
    }
}
