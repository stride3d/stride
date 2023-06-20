// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using Stride.Core.Annotations;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Implements <see cref="SerializationStream"/> as a binary reader.
    /// </summary>
    public class BinarySerializationReader : SerializationStream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinarySerializationReader"/> class.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        public BinarySerializationReader([NotNull] Stream inputStream)
        {
            Reader = new BinaryReader(inputStream);
            UnderlyingStream = inputStream;
        }

        private BinaryReader Reader { get; }

        /// <inheritdoc />
        public override void Serialize(ref bool value)
        {
            var result = UnderlyingStream.ReadByte();
            if (result == -1)
                throw new EndOfStreamException();
            value = result != 0;
        }

        /// <inheritdoc />
        public override unsafe void Serialize(ref float value)
        {
            Span<byte> buffer = MemoryMarshal.Cast<float, byte>(MemoryMarshal.CreateSpan(ref value, 1));
            var read = UnderlyingStream.Read(buffer);
            if (read != sizeof(float))
                throw new EndOfStreamException();
            value = BinaryPrimitives.ReadSingleLittleEndian(buffer);
        }

        /// <inheritdoc />
        public override void Serialize(ref double value)
        {
            Span<byte> buffer = MemoryMarshal.Cast<double, byte>(MemoryMarshal.CreateSpan(ref value, 1));
            var read = (ulong)UnderlyingStream.Read(buffer);
            if (read != sizeof(double))
                throw new EndOfStreamException();
            value = BinaryPrimitives.ReadDoubleLittleEndian(buffer);
        }

        /// <inheritdoc />
        public override void Serialize(ref short value)
        {
            Span<byte> buffer = MemoryMarshal.Cast<short, byte>(MemoryMarshal.CreateSpan(ref value, 1));
            var read = UnderlyingStream.Read(buffer);
            if (read != sizeof(short))
                throw new EndOfStreamException();
            value = BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }

        /// <inheritdoc />
        public override void Serialize(ref int value)
        {
            Span<byte> buffer = MemoryMarshal.Cast<int, byte>(MemoryMarshal.CreateSpan(ref value, 1));
            var read = UnderlyingStream.Read(buffer);
            if (read != sizeof(int))
                throw new EndOfStreamException();
            value = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        /// <inheritdoc />
        public override void Serialize(ref long value)
        {
            Span<byte> buffer = MemoryMarshal.Cast<long, byte>(MemoryMarshal.CreateSpan(ref value, 1));
            var read = UnderlyingStream.Read(buffer);
            if (read != sizeof(long))
                throw new EndOfStreamException();
            value = BinaryPrimitives.ReadInt64LittleEndian(buffer);
        }

        /// <inheritdoc />
        public override void Serialize(ref ushort value)
        {
            Span<byte> buffer = MemoryMarshal.Cast<ushort, byte>(MemoryMarshal.CreateSpan(ref value, 1));
            var read = UnderlyingStream.Read(buffer);
            if (read != sizeof(ushort))
                throw new EndOfStreamException();
            value = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        }

        /// <inheritdoc />
        public override void Serialize(ref uint value)
        {
            Span<byte> buffer = MemoryMarshal.Cast<uint, byte>(MemoryMarshal.CreateSpan(ref value, 1));
            var read = UnderlyingStream.Read(buffer);
            if (read != sizeof(uint))
                throw new EndOfStreamException();
            value = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        /// <inheritdoc />
        public override void Serialize(ref ulong value)
        {
            Span<byte> buffer = MemoryMarshal.Cast<ulong, byte>(MemoryMarshal.CreateSpan(ref value, 1));
            var read = UnderlyingStream.Read(buffer);
            if (read != sizeof(ulong))
                throw new EndOfStreamException();
            value = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }

        /// <inheritdoc />
        public override void Serialize([NotNull] ref string value)
        {
            value = Reader.ReadString();
        }

        /// <inheritdoc />
        public override void Serialize(ref char value)
        {
            value = Reader.ReadChar();
        }

        /// <inheritdoc />
        public override void Serialize(ref byte value)
        {
            Span<byte> buffer = MemoryMarshal.CreateSpan(ref value, 1);
            var read = UnderlyingStream.Read(buffer);
            if (read != sizeof(byte))
                throw new EndOfStreamException();
            value = buffer[0];
        }

        /// <inheritdoc />
        public override void Serialize(ref sbyte value)
        {
            Span<byte> buffer = MemoryMarshal.Cast<sbyte, byte>(MemoryMarshal.CreateSpan(ref value, 1));
            var read = UnderlyingStream.Read(buffer);
            if (read != sizeof(sbyte))
                throw new EndOfStreamException();
            value = (sbyte)buffer[0];
        }

        /// <inheritdoc />
        public override void Serialize([NotNull] byte[] values, int offset, int count)
        {
            Reader.Read(values, offset, count);
        }
        /// <inheritdoc/>
        public override void Serialize(Span<byte> buffer) => UnderlyingStream.Read(buffer);

        /// <inheritdoc />
        public override void Flush()
        {
            UnderlyingStream.Flush();
        }
    }
}
