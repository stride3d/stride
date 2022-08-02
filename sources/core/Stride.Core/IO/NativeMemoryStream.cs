// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Stride.Core.IO
{
    /// <summary>
    /// A <see cref="MemoryStream"/> over a native memory region.
    /// </summary>
    public unsafe class NativeMemoryStream : NativeStream
    {
        private readonly byte* dataStart;
        private readonly byte* dataEnd;
        private byte* dataCurrent;

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeMemoryStream"/> class.
        /// </summary>
        /// <param name="data">The native data pointer.</param>
        /// <param name="length">The data length.</param>
        [Obsolete("Consider using MemoryStream or UnmanagedMemoryStream.")]
        public NativeMemoryStream(IntPtr data, long length)
            : this((byte*)data, length)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeMemoryStream"/> class.
        /// </summary>
        /// <param name="data">The native data pointer.</param>
        /// <param name="length">The data length.</param>
        [Obsolete("Consider using MemoryStream or UnmanagedMemoryStream.")]
        public NativeMemoryStream(byte* data, long length)
        {
            this.dataStart = data;
            this.dataCurrent = data;
            this.dataEnd = data + length;
        }

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    dataCurrent = dataStart + offset;
                    break;
                case SeekOrigin.Current:
                    dataCurrent += offset;
                    break;
                case SeekOrigin.End:
                    dataCurrent = dataEnd + offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }
            return dataCurrent - dataStart;
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Length can't be changed.");
        }

        /// <inheritdoc/>
        public override int Read(Span<byte> buffer)
        {
            Debug.Assert(dataEnd >= dataCurrent);
            var read = (nint)(dataEnd - dataCurrent);
            if (read > buffer.Length) read = buffer.Length;
            var src = new Span<byte>(dataCurrent, (int)read);
            src.CopyTo(buffer);
            dataCurrent += read;
            return (int)read;
        }

        /// <inheritdoc/>
        public override unsafe int Read(byte[] buffer, int offset, int count)
        {
            Debug.Assert(
                (offset | count) >= 0 &&
                (uint)offset + (uint)count <= (uint)buffer.Length);
            var bytesLeft = (int)(dataEnd - dataCurrent);
            if (count > bytesLeft)
                count = bytesLeft;
            fixed (byte* pinned = &buffer[0])
                Unsafe.CopyBlockUnaligned(pinned + offset, dataCurrent, (uint)count);
            dataCurrent += count;
            return count;
        }

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Debug.Assert(dataEnd >= dataCurrent);
            var written = (nint)(dataEnd - dataCurrent);
            if (written < buffer.Length)
                throw new IOException();
            else
                written = buffer.Length;
            var dst = new Span<byte>(dataCurrent, (int)written);
            buffer.CopyTo(dst);
            dataCurrent += written;
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            Debug.Assert(
                (offset | count) >= 0 &&
                (uint)offset + (uint)count <= (uint)buffer.Length);
            var bytesLeft = (int)(dataEnd - dataCurrent);
            if (count > bytesLeft)
                throw new InvalidOperationException("Buffer too small");

            fixed (byte* pinned = &buffer[0])
                Unsafe.CopyBlockUnaligned(dataCurrent, pinned + offset, (uint)count);
            dataCurrent += count;
        }

        /// <inheritdoc/>
        [Obsolete("Use Read(Span<byte>).")]
        public override unsafe int Read(nint buffer, int count)
        {
            Debug.Assert(count >= 0);
            var bytesLeft = (int)(dataEnd - dataCurrent);
            if (count > bytesLeft)
                count = bytesLeft;

            Unsafe.CopyBlockUnaligned((void*)buffer, dataCurrent, (uint)count);
            dataCurrent += count;
            return count;
        }

        /// <inheritdoc/>
        [Obsolete("Use Stream.Write(ReadOnlySpan<byte>).")]
        public override void Write(nint buffer, int count)
        {
            Debug.Assert(count >= 0);
            var bytesLeft = (int)(dataEnd - dataCurrent);
            if (count > bytesLeft)
                throw new InvalidOperationException("Buffer too small");

            Unsafe.CopyBlockUnaligned(dataCurrent, (void*)buffer, (uint)count);
            dataCurrent += count;
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            if (dataCurrent >= dataEnd)
                return -1;

            return *dataCurrent++;
        }

        /// <inheritdoc/>
        public override void WriteByte(byte value)
        {
            if (dataCurrent >= dataEnd)
                throw new InvalidOperationException("Buffer too small");

            *dataCurrent++ = value;
        }

        /// <inheritdoc/>
        [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
        public override ushort ReadUInt16()
        {
            if (dataCurrent + sizeof(ushort) > dataEnd)
                throw new InvalidOperationException("Buffer too small");

            var result = *((ushort*)dataCurrent);
            dataCurrent += sizeof(ushort);
            return result;
        }

        [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
        public override uint ReadUInt32()
        {
            if (dataCurrent + sizeof(uint) > dataEnd)
                throw new InvalidOperationException("Buffer too small");

            var result = *((uint*)dataCurrent);
            dataCurrent += sizeof(uint);
            return result;
        }

        /// <inheritdoc/>
        [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
        public override ulong ReadUInt64()
        {
            if (dataCurrent + sizeof(ulong) > dataEnd)
                throw new InvalidOperationException("Buffer too small");

            var result = *((ulong*)dataCurrent);
            dataCurrent += sizeof(ulong);
            return result;
        }

        /// <inheritdoc/>
        [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
        public override void Write(ushort i)
        {
            if (dataCurrent + sizeof(ushort) > dataEnd)
                throw new InvalidOperationException("Buffer too small");

            *((ushort*)dataCurrent) = i;
            dataCurrent += sizeof(ushort);
        }

        /// <inheritdoc/>
        [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
        public override void Write(uint i)
        {
            if (dataCurrent + sizeof(uint) > dataEnd)
                throw new InvalidOperationException("Buffer too small");

            *((uint*)dataCurrent) = i;
            dataCurrent += sizeof(uint);
        }

        /// <inheritdoc/>
        [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
        public override void Write(ulong i)
        {
            if (dataCurrent + sizeof(ulong) > dataEnd)
                throw new InvalidOperationException("Buffer too small");

            *((ulong*)dataCurrent) = i;
            dataCurrent += sizeof(ulong);
        }

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => true;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override long Length => dataEnd - dataStart;

        /// <inheritdoc/>
        public override long Position
        {
            get => dataCurrent - dataStart;
            set => dataCurrent = dataStart + value;
        }
    }
}
