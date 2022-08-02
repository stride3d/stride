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
    [Obsolete("Consider using MemoryStream or UnmanagedMemoryStream.")]
    public unsafe class NativeMemoryStream : Stream
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
