// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
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
            NativeStream = outputStream;
        }

        private BinaryWriter Writer { get; }

        /// <inheritdoc />
        public override void Serialize(ref bool value)
        {
            NativeStream.WriteByte(value ? (byte)1 : (byte)0);
        }

#pragma warning disable CS0618 // Type or member is obsolete
        /// <inheritdoc />
        public override unsafe void Serialize(ref float value)
        {
            NativeStream.Write(Unsafe.As<float, uint>(ref value));
        }

        /// <inheritdoc />
        public override unsafe void Serialize(ref double value)
        {
            NativeStream.Write(Unsafe.As<double, ulong>(ref value));
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

        /// <inheritdoc />
        public override void Serialize(Span<byte> buffer) => NativeStream.Write(buffer);

        /// <inheritdoc />
        public override void Flush()
        {
            NativeStream.Flush();
        }
    }
}
